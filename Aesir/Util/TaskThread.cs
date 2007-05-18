using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Aesir.Util {
	// TODO: Implement priorities and stuff!
	class TaskThread {
		public delegate object TaskRun();
		public delegate void TaskCompleted(object args);
		private class Task : IComparable {
			public TaskRun taskRun;
			public TaskCompleted taskCompleted;
			public int priority;
			public Task(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
				this.taskRun = taskRun;
				this.taskCompleted = taskCompleted;
				this.priority = priority;
			}
			public int CompareTo(object obj) {
				return priority.CompareTo(((Task)obj).priority);
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
		public void DoTask(TaskRun taskRun, TaskCompleted taskCompleted) {
			DoTask(taskRun, taskCompleted, 0);
		}
		public void DoTask(TaskRun taskRun, TaskCompleted taskCompleted, int priority) {
			Task task = new Task(taskRun, taskCompleted, priority);
			tasks.Push(task);
			UpdateThread();
		}
		private void UpdateThread() {
			if(!thread.IsBusy && tasks.Count > 0)
				thread.RunWorkerAsync(tasks.Pop());
		}
		private void thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args) {
			TaskResult taskResult = (TaskResult)args.Result;
			taskResult.task.taskCompleted(taskResult.result);
			UpdateThread();
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