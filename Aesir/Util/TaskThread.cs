using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using C5;

namespace Aesir.Util {
	public delegate object TaskWorker();
	public delegate void TaskCompleted(object args);
	class InvalidTaskHandleException : Exception {
		public InvalidTaskHandleException(string message) : base(message) { }
		public InvalidTaskHandleException() : base("The task handle is invalid.") { }
	}
	interface ITaskHandle { }
	class TaskThread {
		private class Task {
			private TaskWorker taskWorker;
			private TaskCompleted taskCompleted;
			public void TaskWorker() {
				result = taskWorker();
			}
			public void TaskCompleted() {
				lock(this) {
					if(!cancelled)
						taskCompleted(result);
				}
			}
			private int priority;
			public int Priority {
				get { return priority; }
				set { priority = value; }
			}
			public void Cancel() {
				lock(this) cancelled = true;
			}
			// This flag is used to cancel a task in progress. Access to this flag is synchronized
			// by this.
			private bool cancelled;
			private object result = null; // The return value of taskWorker
			public Task(TaskWorker taskWorker, TaskCompleted taskCompleted, int priority) {
				this.taskWorker = taskWorker;
				this.taskCompleted = taskCompleted;
				this.priority = priority;
			}
		}
		// Used to order tasks based on their priority
		private class TaskComparer : IComparer<Task> {
			public int Compare(Task left, Task right) {
				return left.Priority.CompareTo(right.Priority);
			}
		}
		private class TaskHandle : ITaskHandle {
			public IPriorityQueueHandle<Task> handle;
			public Task task;
			public TaskHandle(Task task, IPriorityQueueHandle<Task> handle) {
				this.task = task;
				this.handle = handle;
			}
		}
		// Used to synchronize access to the tasks collection
		private readonly object SyncRoot = new Object();
		private IPriorityQueue<Task> tasks;
		private BackgroundWorker thread = new BackgroundWorker();
		public TaskThread() {
			tasks = new IntervalHeap<Task>(new TaskComparer());
			thread.RunWorkerCompleted += thread_RunWorkerCompleted;
			thread.DoWork += thread_DoWork;
		}
		#region BackgroundWorker event handlers
		private void thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args) {
			((Task)args.Result).TaskCompleted();
			UpdateBackgroundWorker();
		}
		private void thread_DoWork(object sender, DoWorkEventArgs args) {
			((Task)args.Argument).TaskWorker();
			args.Result = args.Argument;
			if(thread.CancellationPending) args.Cancel = true;
		}
		#endregion
		public ITaskHandle AddTask(TaskWorker taskWorker, TaskCompleted taskCompleted) {
			return AddTask(taskWorker, taskCompleted, 0);
		}
		public ITaskHandle AddTask(TaskWorker taskWorker, TaskCompleted taskCompleted, int priority) {
			Task task = new Task(taskWorker, taskCompleted, priority);
			IPriorityQueueHandle<Task> handle = null;
			lock(SyncRoot) tasks.Add(ref handle, task);
			UpdateBackgroundWorker();
			return new TaskHandle(task, handle);
		}
		/// <exception cref="InvalidTaskHandleException" />
		public void PromoteTask(ITaskHandle taskHandle, int priority) {
			IPriorityQueueHandle<Task> handle = ((TaskHandle)taskHandle).handle;
			Task task;
			lock(SyncRoot) {
				if(!tasks.Find(handle, out task)) throw new InvalidTaskHandleException();
				if(priority > task.Priority) return; // Don't allow clients to demote tasks
				tasks.Delete(handle);
				task.Priority = priority;
				tasks.Add(ref handle, task);
			}
			((TaskHandle)taskHandle).handle = handle;
		}
		public void CancelTask(ITaskHandle taskHandle) {
			IPriorityQueueHandle<Task> handle = ((TaskHandle)taskHandle).handle;
			try {
				lock(SyncRoot) tasks.Delete(handle);
			} catch(InvalidPriorityQueueHandleException) {
				// Silence the error. The task might be currently executing. Calling Cancel() should
				// handle it.
			}
			((TaskHandle)taskHandle).task.Cancel();
		}
		private void UpdateBackgroundWorker() {
			lock(SyncRoot) {
				if(!thread.IsBusy && tasks.Count > 0)
					thread.RunWorkerAsync(tasks.DeleteMin());
			}
		}
	}
}