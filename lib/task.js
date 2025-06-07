// task.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Task API (Time-sharing based `async`, `setTimeout`, `setInterval`, implementation in WSH.js)
//
// example:
// var TASK = require("lib/task");
// var taskQueue = TASK.createTaskQueue();
// TASK.putTask(queue, TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(100); }, [1, 2, 3]))
//     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(200); }, [4, 5, 6]))
//     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(300); }, [7, 8, 9]))
// ;
// TASK.putTask(queue, TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(100); }, [3, 2, 1])
//     .then(TASK.createTask(function(task, a, b, c) { console.log(a + b + c); sleep(200); }, [6, 5, 4]))
//     .then(TASK.createTask(function(task, a, b, c) { TASK.stop(); console.log(a + b + c); sleep(300); }, [9, 8, 7]))
// ;
// taskQueue.run();
// 
function Task(f, params) {
    this.f = f;
    this.params = params;
    this.nextTask = null;
    this.failTask = null;
    this.when = 0;
    this.delay = 0;
    this.callCount = 0;

    this.setNextTask = function(task) {
        this.nextTask = task;
        return this;
    };
    this.setFailTask = function(task) {
        this.failTask = task;
        return this;
    };
    this.setWhen = function(when) {
        this.when = when;
        return this;
    };
    this.setDelay = function(delay) {
        this.delay = delay;
        return this;
    };
    this.upCallCount = function() {
        this.callCount++;
        return this;
    };
    this.resetCount = function() {
        this.callCount = 0;
        return this;
    };
}

function TaskQueue() {
    this._task = null;
    this._keepalive = true;
    this.queue = [];

    this.put = function(task, delay) {
        var now = new Date();

        try {
            task.setWhen(now.getTime());
            if (typeof(delay) !== "undefined") {
                task.setDelay(delay);
            }
            this._task = task;
            this.queue.push(task);
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
                var delta = this.queue.map(function(task) {
                    return task.when + task.delay - now.getTime();
                });
                var i = delta.indexOf(Math.min.apply(Math, delta));
                var d = this.queue[i].when + this.queue[i].delay - now.getTime();
                if (i > -1 && d <= 0) {
                    console.log("Task", i, d);
                    task = this.queue.splice(i, 1)[0];
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
                    result = task.f.apply(Task, [task].concat(task.params));
                    if (task.nextTask != null) {
                        this.put(task.nextTask, task.nextTask.delay);
                    }
                    task.upCallCount();  // 호출 횟수 기록
                } catch (e) {
                    console.error("Task exception: " + e.message);
                    console.error("task.f: " + typeof(task.f));
                    console.error("task.params: " + typeof(task.params));
                    
                    // 태스크 개별 오류 처리
                    if (task.failTask) {
                        this.put(task.failTask, 0);
                    }
                }
            }
        } catch(e) {
            console.error("TaskQueue.next: " + e.message);
        }

        return result;
    };
    this.then = function(task, delay) {
        try {
            if (typeof(delay) !== "undefined") {
                task.setDelay(delay);
            }
            this._task.setNextTask(task);
            this._task = task;
            return this;
        } catch(e) {
            console.error("TaskQueue.then: " + e.message);
            console.error(this._task);
        }
    };
    this.run = function() {
        this._keepalive = true;
        while(this._keepalive) {
            this.next();
            sleep(1);
        }
    };
    this.stop = function() {
        this._keepalive = false;
    };
}

function createTaskQueue() {
    return new TaskQueue();
}

function createTask(f, params, failTask) {
    try {
        return (new Task(f, params)).setFailTask(failTask);
    } catch(e) {
        console.error("createTask exception: " + e.message);
    }
}

function putTask(q, task, delay) {
    try {
        if (q instanceof TaskQueue && task instanceof Task) {
            return q.put(task, delay);
        }
    } catch(e) {
        console.error("putTask exception: " + e.message);
    }
}

function nextTask(q) {
    try {
        return q.next();
    } catch(e) {
        console.error("nextTask exception: " + e.message);
    }
}

function run(q) {
    q.run();
}

function stop(q) {
    q.stop();
}

////////////////////////////////////////////////////
// START Global functions
////////////////////////////////////////////////////
var __taskQueue__ = new TaskQueue();

function setTimeout(f, delay) {
    var params = (arguments.length > 2 ? arguments.slice(2) : []);
    var task = new Task(f, delay);
    __taskQueue__.put(task);
}

// TODO: compatiblity with clearInterval()
function setInterval(f, delay) {
    var params = (arguments.length > 2 ? arguments.slice(2) : []);
    var task = new Task(f, delay);
    task.setNextTask(task);
    __taskQueue__.put(task);
}

exports.__taskQueue__ = __taskQueue__;
exports.setTimeout = setTimeout;
exports.setInterval = setInterval;

////////////////////////////////////////////////////
// END Global functions
////////////////////////////////////////////////////

exports.Task = Task;
exports.TaskQueue = TaskQueue;

exports.createTaskQueue = createTaskQueue;
exports.createTask = createTask;
exports.putTask = putTask;
exports.nextTask = nextTask;
exports.run = run;
exports.stop = stop;

exports.VERSIONINFO = "Task Module (task.js) version 0.2.2";
exports.global = global;
exports.require = require;
