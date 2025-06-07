// fsm.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// FSM(Finite State Machine) library for WelsonJS framework
// 
// State constructor function
function State(name) {
    this.name = name;
    this.transitions = {};
}
State.NONE = "none";

// Transition constructor function
function Transition(fromState, toState) {
    STD.EventTarget.call(this);  // Ensure proper inheritance
    this.fromState = fromState;
    this.toState = toState;
}
Transition.prototype = Object.create(STD.EventTarget.prototype);
Transition.prototype.constructor = Transition;

// FSM constructor function
function FSM() {
    this.states = {};
    this.currentState = null;
    this.previousState = null;
}

// Method to add a state
FSM.prototype.addState = function(stateName) {
    if (!this.states[stateName]) {
        this.states[stateName] = new State(stateName);
    }
};

// Method to add a transition between states
FSM.prototype.addTransition = function(fromStateName, toStateName, eventType, eventListener) {
    if (!(fromStateName in this.states) || !(toStateName in this.states)) {
        console.error('Invalid state names provided.');
        return false;
    }
    
    if (typeof eventListener !== "function") {
        console.error('Event listener must be a function.');
        return false;
    }

    var transition = new Transition(this.states[fromStateName], this.states[toStateName]);
    transition.addEventListener(eventType, eventListener);
    this.states[fromStateName].transitions[toStateName] = transition;

    return true;
}

// Method to set the current state
FSM.prototype.setState = function(stateName) {
    if (!(stateName in this.states)) {
        console.error('Invalid state name provided.');
        return false;
    }
    
    this.previousState = this.currentState;
    this.currentState = this.states[stateName];

    return true;
};

// Method to attempt a state transition
FSM.prototype.tryTransition = function(stateName, eventType, options) {
    if (this.currentState && stateName in this.currentState.transitions) {
        var transition = this.currentState.transitions[stateName];
        var event = new STD.Event(eventType, options);
        transition.dispatchEvent(event);
        this.setState(stateName);
    } else {
        console.error('Transition not possible.');
    }
};

exports.State = State;
exports.FSM = FSM;

exports.VERSIONINFO = "FSM(Finite State Machine) library (fsm.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
