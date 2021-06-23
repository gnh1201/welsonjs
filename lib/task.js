//////////////////////////////////////////////////////////////////////////////////
// Task API (Time-sharing based `async` implementation in WSH.js)
/////////////////////////////////////////////////////////////////////////////////

var Task = function(id, f, params) {
	this.id = id;
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
	this.lastId = 0;
	this.queue = [];
	this.put = function(task) {
		try {
			this._task = task;
			this.queue.push(this._task);
			this.lastId++;
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

exports.buildTask = function(q, f, params) {
	try {
		if (q instanceof TaskQueue) {
			return new Task(q.lastId, f, params);
		}
	} catch(e) {
		console.error("makeTask exception: " + e.message);
	}
};

exports.putTask = function(q, f, params) {
	try {
		if (q instanceof TaskQueue) {
			return q.put(new Task(q.lastId, f, params));
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
