//////////////////////////////////////////////////////////////////////////////////
// Task API (Time-sharing based `async` implementation in WSH.js)
/////////////////////////////////////////////////////////////////////////////////

/* // example:
 * // var TASK = require("lib/task");
 * // var queue = TASK.createTaskQueue();
 * // TASK.putTask(queue, function() { console.log('123'); }, [1, 2, 3])
 * //     .then(createTask(function() { console.log('456');  }, [4, 5, 6]))
 * //     .then(createTask(function() { TASK.stop(); console.log('789'); }, [7, 8, 9]))
 * // ;
 * // queue.run();
 */

var Task = function(f, params) 
	this.f = f;
	this.params = params;
	this.nextTask = null;
	this.setNextTask = function(task) {
		this.nextTask = task;
	}
};

var TaskQueue = function() {
	this._task = null;
	this._keepalive = true;
	this.queue = [];
	this.put = function(task) {
		try {
			this._task = task;
			this.queue.push(this._task);
		} catch(e) {
			console.error("TaskQueue.put exception: " + e.message);
		}

		return this;
	};
	this.get = function() {
		var task = null;
		
		try {
			if (this.queue.length > 0) {
				task = this.queue[0];
				this.queue = this.queue.slice(1);
			}
		} catch(e) {
			console.error("TaskQueue.get: " + e.message);
		}
		
		return task;
	};
	this.next = function() {
		var result = null;
		
		try {
			var task = this.get();

			if (task != null) {
				try {
					result = task.f.apply(null, task.params);
				} catch (e) {
					console.error("Task exception: " + e.message);
				}
				
				if (task.nextTask != null) {
					this.put(task.nextTask);
				}
			}
		} catch(e) {
			console.error("TaskQueue.next: " + e.message);
		}

		return result;
	};
	this.then = function(task) {
		try {
			this._task.setNextTask(task);
			this._task = task;
			return this;
		} catch(e) {
			console.error("TaskQueue.then: " + e.message);
		}
	};
	this.run = function() {
		this._keepalive = true;
		while(this._keepalive) {
			this.next();
			sleep(100);
		}
	};
	this.stop = function() {
		this._keepalive = false;
	};
};

exports.createTaskQueue = function() {
	return new TaskQueue();
};

exports.createTask = function(f, params) {
	try {
		return new Task(f, params);
	} catch(e) {
		console.error("createTask exception: " + e.message);
	}
};

exports.putTask = function(q, f, params) {
	try {
		if (q instanceof TaskQueue) {
			return q.put(new Task(f, params));
		}
	} catch(e) {
		console.error("putTask exception: " + e.message);
	}
};

exports.nextTask = function(q) {
	try {
		return q.next();
	} catch(e) {
		console.error("nextTask exception: " + e.message);
	}
};

exports.run = function(q) {
	q.run();
};

exports.stop = function(q) {
	q.stop();
}
