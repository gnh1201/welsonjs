//////////////////////////////////////////////////////////////////////////////////
// Task API (Time-sharing based `async`, `setTimeout`, `setInterval`, `Promise` implementation in WSH.js)
/////////////////////////////////////////////////////////////////////////////////

/* // example:
 * // var TASK = require("lib/task");
 * // var taskQueue = TASK.createTaskQueue();
 * // TASK.putTask(queue, TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(100); }, [1, 2, 3]))
 * //     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(200); }, [4, 5, 6]))
 * //     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(300); }, [7, 8, 9]))
 * // ;
 * // TASK.putTask(queue, TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(100); }, [3, 2, 1])
 * //     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(200); }, [6, 5, 4]))
 * //     .then(TASK.createTask(function(task, a, b, c) { TASK.stop(); console.log(a + b + c); sleep(300); }, [9, 8, 7]))
 * // ;
 * // taskQueue.run();
 */

var Task = function(f, params) {
	this.f = f;
	this.params = params;
	this.nextTask = null;
	this.when = 0;
	this.setNextTask = function(task) {
		this.nextTask = task;
	};
	this.setWhen = function(when) {
		this.when = when;
	};
	this.clone = function() {
		var task = new Task(this.f, this.params);
		task.setNextTask(this.nextTask);
		task.setWhen(this.when);
		return task;
	};
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
		var now = new Date();

		try {
			if (this.queue.length > 0) {
				for (var i = 0; i < this.queue.length; i++) {
					var delta = this.queue[i].when - now.getTime();
					if (delta <= 0) {
						task = this.queue.splice(i, 1)[0];
						break;
					}
				}
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
					result = task.f.apply(null, [task].concat(task.params));
					if (task.nextTask != null) {
						this.put(task.nextTask);
					}
				} catch (e) {
					console.error("Task exception: " + e.message);
					console.error("task.f: " + typeof(task.f));
					//WScript.echo(task.f);
					console.error("task.params: " + typeof(task.params));
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

exports.putTask = function(q, task) {
	try {
		if (q instanceof TaskQueue && task instanceof Task) {
			return q.put(task);
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
	} else if (args.length == 2) {
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

	if (args.length > 2) {
		f = args[0];
		delay = args[1];
		params = args.slice(2);
	} else if (args.length == 2) {
		f = args[0];
		delay = args[1];
	}

	if (f != null) {
		var when = new Date().getTime() + delay;
		// ... todo ...
	}
};

exports.Promise = function() {
	// ... todo ...
};
*/

exports.VERSIONINFO = "Task Module (task.js) version 0.1";
exports.global = global;
exports.require = require;
