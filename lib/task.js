//////////////////////////////////////////////////////////////////////////////////
// Task API (Time-sharing based `async` implementation in WSH.js)
/////////////////////////////////////////////////////////////////////////////////

/* // example:
 * // var TASK = require("lib/task");
 * // var taskQueue = TASK.createTaskQueue();
 * // TASK.putTask(queue, function() { console.log('123'); sleep(100); }, [1, 2, 3])
 * //     .then(TASK.createTask(function() { console.log('456'); sleep(200); }, [4, 5, 6]))
 * //     .then(TASK.createTask(function() { console.log('789'); sleep(300); }, [7, 8, 9]))
 * // ;
 * // TASK.putTask(queue, function() { console.log('321'); sleep(100); }, [3, 2, 1])
 * //     .then(TASK.createTask(function() { console.log('654'); sleep(200); }, [6, 5, 4]))
 * //     .then(TASK.createTask(function() { TASK.stop(); console.log('987'); sleep(300); }, [9, 8, 7]))
 * // ;
 * // taskQueue.run();
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
			var now = new Date().getTime();
			this.next();
			sleep(1);
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
};

/*
exports.setTimeout = function() {
	var f = null;
	var delay = 0;
	var params = [];
	var args = arguments;

	if (args.length > 2) {
		f = arguments[0];
		delay = args[1];
		params = args.slice(2);
	} else if (arguments.length == 2) {
		f = args[0];
		delay = args[1];
	}

	if (f != null) {
		var when = new Date().getTime() + delay;
		// ... todo ...
	}
};

exports.setInterval = function() {
	var f = null;
	var delay = 0;
	var params = [];
	var args = arguments;

	if (arguments.length > 2) {
		f = args[0];
		delay = args[1];
		params = args.slice(2);
	} else if (arguments.length == 2) {
		f = args[0];
		delay = args[1];
	}

	if (f != null) {
		var when = new Date().getTime() + delay;
		// ... todo ...
	}
};
*/
