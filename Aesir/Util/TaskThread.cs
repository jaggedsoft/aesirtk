using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using C5;

namespace Aesir.Util {
	class TaskThread {
		public delegate object TaskRun();
		public delegate void TaskCompleted(object args);
		internal class Task : IComparable {
			public TaskRun taskRun;
			public TaskCompleted taskCompleted;
			public int priority;
			public IPriorityQueueHandle<Task> priorityQueueHandle;
			public Task(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
				this.taskRun = taskRun;
				this.taskCompleted = taskCompleted;
				this.priority = priority;
				number = nextNumber;
				nextNumber++;
			}
			private int number;
			private static int nextNumber = 0;
			public int CompareTo(object obj) {
				return priority.CompareTo(((Task)obj).priority);
			}
			public override bool Equals(object obj) {
				Task taskObj = obj as Task;
				if(taskObj == null) return false;
				return taskObj.number == number;
			}
			public override int GetHashCode() {
				return priority.GetHashCode();
			}
			public override string ToString() {
				return "{Priority:" + priority + "}";
			}
		}
		private class TaskResult {
			public object result;
			public Task task;
			public TaskResult(object result, Task task) {
				this.result = result;
				this.task = task;
			}
		}
		public class TaskHandle {
			internal IPriorityQueueHandle<Task> priorityQueueHandle;
			internal TaskHandle(IPriorityQueueHandle<Task> priorityQueueHandle) {
				this.priorityQueueHandle = priorityQueueHandle;
			}
		}
		public TaskThread() {
			thread.RunWorkerCompleted +=new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted);
			thread.DoWork += new DoWorkEventHandler(thread_DoWork);
		}
		public TaskHandle AddTask(TaskRun taskRun, TaskCompleted taskCompleted) {
			return AddTask(taskRun, taskCompleted, 0);
		}
		public TaskHandle AddTask(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
			Task task = new Task(taskRun, taskCompleted, priority);
			lock(syncRoot) tasks.Add(ref task.priorityQueueHandle, task);
			UpdateBackgroundWorker();
			return new TaskHandle(task.priorityQueueHandle);
		}
		public void PromoteTask(TaskHandle taskHandle, int priority) {
			Task task;
			if(!tasks.Find(taskHandle.priorityQueueHandle, out task)) return;
			if(priority > task.priority) return;
			lock(syncRoot) {
				tasks.Delete(taskHandle.priorityQueueHandle);
				task.priority = priority;
				tasks.Add(task);
			}
		}
		public void CancelTask(TaskHandle taskHandle) {
			try {
				lock(syncRoot) tasks.Delete(taskHandle.priorityQueueHandle);
			}  catch(InvalidPriorityQueueHandleException) { }
		}
		private void UpdateBackgroundWorker() {
			lock(syncRoot) {
				if(!thread.IsBusy && tasks.Count > 0)
					thread.RunWorkerAsync(tasks.DeleteMin());
			}
		}
		private void thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args) {
			TaskResult taskResult = (TaskResult)args.Result;
			taskResult.task.taskCompleted(taskResult.result);
			UpdateBackgroundWorker();
		}
		private void thread_DoWork(object sender, DoWorkEventArgs args) {
			Task task = (Task)args.Argument;
			args.Result = new TaskResult(task.taskRun(), task);
			if(thread.CancellationPending) args.Cancel = true;
		}
		private readonly object syncRoot = new Object();
		private IPriorityQueue<Task> tasks = new IntervalHeap<Task>();
		private BackgroundWorker thread = new BackgroundWorker();
	}
}