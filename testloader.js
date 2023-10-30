// testloader

var FILE = require("lib/file");

var profile = JSON.parse(FILE.readFile("data/test-oss-20231030.json", FILE.CdoCharset.CdoUTF_8));

// implement the tests
var test_implements = {
    "es5_polyfills": function() {},

    "registry_find_provider": function() {},

    "registry_read": function() {},

    "registry_write": function() {},

    "wmi_create_object": function() {},
    
    "wmi_execute_query": function() {},

    "wmi_result_query": function() {},

    "shell_create_object": function() {},

    "shell_build_command_line": function() {},

    "shell_set_charset": function() {},

    "shell_working_directory": function() {},

    "shell_create_process": function() {},

    "shell_execute": function() {},

    "shell_run": function() {},

    "shell_run_as": function() {},

    "shell_find_my_documents": function() {},

    "shell_release": function() {},

    "powershell_set_command": function() {},

    "powershell_set_file": function() {},

    "powershell_set_uri": function() {},

    "powershell_execute": function() {},

    "powershell_run_as": function() {},
    
    "system_resolve_env": function() {},

    "system_check_as": function() {},

    "system_get_os_version": function() {},

    "system_get_architecture": function() {},

    "system_get_uuid": function() {},

    "system_get_working_directory": function() {},

    "system_get_script_directory": function() {},

    "system_get_network_interfaces": function() {},

    "system_get_process_list": function() {},

    "system_get_process_list_by_name": function() {},
    
    "system_register_uri": function() {},

    "system_pipe_ipc": function() {},

    "vhid_find_window": function() {},

    "vhid_send_click": function() {},

    "vhid_send_keys": function() {},

    "vhid_send_key_enter": function() {},

    "vhid_send_key_functions": function() {},

    "vhid_alert": function() {},

    "vhid_confirm": function() {},

    "network_http_get": function() {},

    "network_http_post": function() {},

    "network_http_extended": function() {},

    "network_attach_debugger": function() {},

    "network_detect_charset": function() {},

    "network_detect_http_ssl": function() {},

    "network_send_icmp": function() {},

    "extramath_dtm": function() {},

    "extramath_cosine_similarity": function() {},

    "base64_encode": function() {},

    "base64_decode": function() {},

    "chromium_run": function() {},

    "chromium_create_profile": function() {},

    "chromium_run_incognito": function() {},

    "chromium_navigate": function() {},

    "chromium_get_active_pages": function() {},

    "chromium_find_page_by_id": function() {},

    "chromium_find_pages_by_title": function() {},

    "chromium_move_focused": function() {},

    "chromium_adjust_window_size": function() {},

    "chromium_get_element_position": function() {},

    "chromium_get_mapreduced_element_position": function() {},

    "chromium_set_value_to_textbox": function() {},

    "chromium_send_click": function() {},

    "chromium_send_keys": function() {},

    "chromium_auto_scroll_until_end": function() {},

    "grpc_run_server": function() {},

    "grpc_receive_command": function() {},

    "gui_check": function() {}
};

function main(args) {
    var test_id = args[0];

    if (test_id in test_implements) {
        var test = profile.tests.find(function(x) {
            return (x.id == test_id);
        });
        var description = test.description;

        console.log("Will be test: " + description);

        try {
            test_implements[test_id]();
        } catch (e) {
            console.error("ERROR: " + e.message)
        }

        console.log("Test ended");
    }

    console.log(args.join(", "));
}

exports.main = main;
