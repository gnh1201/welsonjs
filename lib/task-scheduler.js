// task-scheduler.js
// https://github.com/gnh1201/welsonjs

// Connect to Task Scheduler service
function connect() {
    var service = CreateObject("Schedule.Service");
    service.Connect();
    return service;
}

// Get the root folder of Task Scheduler
function getRootFolder(service) {
    return service.GetFolder("\\");
}

// Create a new task definition
function createTaskDefinition(service) {
    return service.NewTask(0); // Flags = 0
}

// Set registration info for the task
function setRegistrationInfo(taskDefinition, description, author) {
    var regInfo = taskDefinition.RegistrationInfo;
    regInfo.Description = description;
    regInfo.Author = author;
}

// Configure task settings
function configureTaskSettings(taskDefinition) {
    var settings = taskDefinition.Settings;
    settings.StartWhenAvailable = true;
}

// Create a registration trigger
function createRegistrationTrigger(taskDefinition) {
    var triggers = taskDefinition.Triggers;
    var trigger = triggers.Create(7); // 7 = Registration Trigger
    trigger.ExecutionTimeLimit = "PT5M"; // Five minutes
    trigger.Id = "RegistrationTriggerId";
}

// Add an action to execute a program
function addExecutableAction(taskDefinition, executablePath) {
    var actions = taskDefinition.Actions;
    var action = actions.Create(0); // 0 = Executable Action
    action.Path = executablePath;
}

// Register the task in Task Scheduler
function registerTask(rootFolder, taskDefinition, taskName) {
    rootFolder.RegisterTaskDefinition(
        taskName,
        taskDefinition,
        6,    // Task creation flags
        null, // UserId
        null, // Password
        3     // LogonType
    );
}

// Create and register the task
function createAndRegisterTask(service, rootFolder, taskName, executablePath, description, author) {
    var taskDefinition = createTaskDefinition(service);

    setRegistrationInfo(taskDefinition, description, author);
    configureTaskSettings(taskDefinition);
    createRegistrationTrigger(taskDefinition);
    addExecutableAction(taskDefinition, executablePath);

    console.log("Task definition created. About to submit the task...");
    registerTask(rootFolder, taskDefinition, taskName);
    console.log("Task submitted.");
}

function test() {
    var service = connectToService();
    var rootFolder = getRootFolder(service);

    createAndRegisterTask(
        service,
        rootFolder,
        "Test Registration Trigger",
        "C:\\Windows\\System32\\notepad.exe",
        "Start Notepad when the task is registered.",
        "Author Name"
    );
}

exports.connect = connect;
exports.getRootFolder = getRootFolder;
exports.createAndRegisterTask = createAndRegisterTask;

exports.VERSIONINFO = "Windows Task Scheduler Scripting Interface (task-scheduler.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
