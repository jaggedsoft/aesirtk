using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Aesir.Util {
	class TaskThread {
		public delegate object TaskRun();
		public delegate void TaskCompleted(object args);
		public class Task : IComparable {
			internal TaskRun taskRun;
			internal TaskCompleted taskCompleted;
			internal int priority;
			internal Task(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
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
		public TaskThread() {
			thread.RunWorkerCompleted +=new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted);
			thread.DoWork += new DoWorkEventHandler(thread_DoWork);
		}
		public void AddTask(TaskRun taskRun, TaskCompleted taskCompleted) {
			AddTask(taskRun, taskCompleted, 0);
		}
		public Task AddTask(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
			Task task = new Task(taskRun, taskCompleted, priority);
			lock(tasks.SyncRoot) tasks.Push(task);
			UpdateBackgroundWorker();
			return task;
		}
		public void PromoteTask(Task task, int priority) {
			if(priority > task.priority) return;
			lock(tasks.SyncRoot) {
				tasks.Remove(task);
				task.priority = priority;
				tasks.Push(task);
			}
		}
		public void CancelTask(Task task) {
			lock(tasks.SyncRoot) tasks.Remove(task);
		}
		private void UpdateBackgroundWorker() {
			lock(tasks.SyncRoot) {
				if(!thread.IsBusy && tasks.Count > 0)
					thread.RunWorkerAsync(tasks.Pop());
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
		private IPriorityQueue tasks = new BinaryPriorityQueue();
		private BackgroundWorker thread = new BackgroundWorker();
	}
}