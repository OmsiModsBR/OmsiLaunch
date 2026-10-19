namespace OmsiLaunch.Api;

// Canonical catalog of every OmsiLaunch error (OL_E_) and warning (OL_W_) code
// that the product can emit through diagnostics, exceptions, control replies or
// runtime results. A documentation gate verifies that every code found in the
// sources is listed here and that every listed code is documented.
public static class PublicErrorCodes
{
    // Cli
    public const string CANCELLED = "OL_E_CANCELLED";
    public const string INTERNAL = "OL_E_INTERNAL";
    public const string TIMEOUT = "OL_E_TIMEOUT";
    public const string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING";
    public const string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED";
    // Compatibility
    public const string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED";
    public const string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED";
    public const string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD";
    public const string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM";
    public const string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE";
    // Content
    public const string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND";
    public const string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED";
    public const string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED";
    public const string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND";
    public const string HOF_CONFLICT = "OL_E_HOF_CONFLICT";
    public const string HOF_INVALID = "OL_E_HOF_INVALID";
    public const string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE";
    public const string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND";
    public const string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND";
    public const string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND";
    public const string NOT_FOUND = "OL_E_NOT_FOUND";
    public const string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND";
    public const string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND";
    public const string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND";
    public const string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND";
    // Installation
    public const string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY";
    public const string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND";
    public const string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE";
    public const string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH";
    public const string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE";
    public const string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING";
    public const string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING";
    public const string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID";
    // InvalidArgument
    public const string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT";
    public const string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE";
    public const string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE";
    public const string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING";
    // LaunchSpec
    public const string SPEC_INVALID = "OL_E_SPEC_INVALID";
    public const string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND";
    public const string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE";
    public const string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY";
    // LocalControl
    public const string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN";
    public const string CONTROL_FAILED = "OL_E_CONTROL_FAILED";
    public const string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED";
    public const string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID";
    public const string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE";
    public const string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL";
    public const string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE";
    public const string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH";
    // Other
    public const string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE";
    // Presentation
    public const string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID";
    public const string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING";
    public const string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED";
    public const string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH";
    public const string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING";
    public const string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING";
    public const string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED";
    // Process
    public const string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED";
    public const string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED";
    public const string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY";
    public const string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED";
    public const string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION";
    public const string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED";
    public const string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED";
    // Runtime
    public const string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED";
    public const string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED";
    public const string WEATHER_INVALID = "OL_E_WEATHER_INVALID";
    public const string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED";
    public const string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND";
    public const string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE";
    public const string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO";
    public const string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED";
    public const string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED";
    public const string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED";
    public const string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING";
    public const string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE";
    public const string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID";
    public const string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY";
    public const string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED";
    public const string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID";
    public const string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE";
    public const string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND";
    public const string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID";
    public const string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION";
    public const string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE";
    public const string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY";
    public const string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID";
    public const string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND";
    public const string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE";
    public const string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE";
    public const string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED";
    public const string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE";
    public const string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED";
    public const string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE";
    public const string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN";
    public const string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE";
    public const string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH";
    public const string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED";
    public const string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT";
    public const string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID";
    public const string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE";
    public const string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE";
    public const string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH";
    public const string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT";
    public const string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE";
    public const string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND";
    public const string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID";
    public const string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE";
    public const string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID";
    public const string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND";
    public const string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE";
    public const string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED";
    // RuntimeD3D
    public const string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST";
    public const string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT";
    public const string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER";
    public const string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT";
    public const string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED";
    public const string D3D_NOT_READY = "OL_E_D3D_NOT_READY";
    public const string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS";
    public const string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED";
    public const string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE";
    // Session
    public const string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE";
    public const string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED";
    public const string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION";
    public const string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED";
    public const string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH";
    public const string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE";
    public const string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING";
    public const string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID";
    public const string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED";
    public const string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED";
    public const string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT";
    public const string START_SESSION = "OL_E_START_SESSION";
    public const string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED";
    // Module
    public const string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID";
    public const string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND";
    public const string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY";
    public const string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED";
    public const string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED";
    public const string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING";
    public const string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED";
    public const string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED";
    public const string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED";
    public const string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT";
    public const string MODULE_DISABLED = "OL_E_MODULE_DISABLED";
    public const string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED";
    public const string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN";
    public const string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED";
    public const string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE";
    public const string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED";
    public const string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE";
    public const string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID";
    public const string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT";
    public const string MODULE_OPERATION_NOT_DECLARED = "OL_E_MODULE_OPERATION_NOT_DECLARED";
    public const string MODULE_CONTENT_INPUT_UNDECLARED = "OL_E_MODULE_CONTENT_INPUT_UNDECLARED";
    public const string MODULE_CONTENT_INPUT_INVALID = "OL_E_MODULE_CONTENT_INPUT_INVALID";
    public const string UI_NOT_PAUSED = "OL_E_UI_NOT_PAUSED";
    public const string UI_BUSY = "OL_E_UI_BUSY";
    public const string UI_ALREADY_TAKEN_OVER = "OL_E_UI_ALREADY_TAKEN_OVER";
    // SessionProfile
    public const string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING";
    public const string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID";
    public const string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH";
    public const string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND";
    public const string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT";
    public const string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE";
    public const string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND";
    public const string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED";
    public const string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE";
    public const string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN";
    // Transaction
    public const string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED";
    public const string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH";
    public const string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED";
    public const string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT";
    public const string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING";
    public const string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED";
    public const string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED";
    public const string RESTORE_FAILED = "OL_E_RESTORE_FAILED";
    // Warning
    public const string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED";
    public const string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED";
    public const string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE";
    public const string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING";
    public const string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED";
    public const string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED";

    public static readonly IReadOnlyList<PublicErrorDescriptor> All = new PublicErrorDescriptor[]
    {
        new("OL_E_CANCELLED", "Cli"),
        new("OL_E_INTERNAL", "Cli"),
        new("OL_E_TIMEOUT", "Cli"),
        new("OL_E_WINDOWS_HOST_MISSING", "Cli"),
        new("OL_E_WINDOWS_HOST_START_FAILED", "Cli"),
        new("OL_E_BUILD_VALIDATION_FAILED", "Compatibility"),
        new("OL_E_UNSUPPORTED_BUILD", "Compatibility"),
        new("OL_E_CAPABILITY_UNRESOLVED", "Compatibility"),
        new("OL_E_UNSUPPORTED_OPERATING_SYSTEM", "Compatibility"),
        new("OL_E_UNSUPPORTED_OS_ARCHITECTURE", "Compatibility"),
        new("OL_E_ENTRYPOINT_NOT_FOUND", "Content"),
        new("OL_E_ENTRYPOINT_REQUIRED", "Content"),
        new("OL_E_ENTRYPOINT_NOT_PRESENTED", "Content"),
        new("OL_E_HOF_NOT_FOUND", "Content"),
        new("OL_E_HOF_CONFLICT", "Content"),
        new("OL_E_HOF_INVALID", "Content"),
        new("OL_E_HOF_PATH_ESCAPE", "Content"),
        new("OL_E_HOF_SOURCE_NOT_FOUND", "Content"),
        new("OL_E_HOF_TARGET_NOT_FOUND", "Content"),
        new("OL_E_MAP_NOT_FOUND", "Content"),
        new("OL_E_NOT_FOUND", "Content"),
        new("OL_E_REPAINT_NOT_FOUND", "Content"),
        new("OL_E_SITUATION_MAP_NOT_FOUND", "Content"),
        new("OL_E_SITUATION_NOT_FOUND", "Content"),
        new("OL_E_VEHICLE_NOT_FOUND", "Content"),
        new("OL_E_INSTALLATION_BUSY", "Installation"),
        new("OL_E_INSTALLATION_NOT_FOUND", "Installation"),
        new("OL_E_INSTALLATION_NOT_WRITABLE", "Installation"),
        new("OL_E_PERMANENT_PLUGIN_HASH_MISMATCH", "Installation"),
        new("OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE", "Installation"),
        new("OL_E_PERMANENT_PLUGIN_MISSING", "Installation"),
        new("OL_E_PLATFORM_CAPABILITY_MISSING", "Installation"),
        new("OL_E_RELEASE_MANIFEST_INVALID", "Installation"),
        new("OL_E_INVALID_ARGUMENT", "InvalidArgument"),
        new("OL_E_INVALID_SETTING_VALUE", "InvalidArgument"),
        new("OL_E_SETTING_NOT_WRITABLE", "InvalidArgument"),
        new("OL_E_UNKNOWN_SETTING", "InvalidArgument"),
        new("OL_E_SPEC_INVALID", "LaunchSpec"),
        new("OL_E_SPEC_NOT_FOUND", "LaunchSpec"),
        new("OL_E_SPEC_TOO_LARGE", "LaunchSpec"),
        new("OL_E_SPEC_UNKNOWN_PROPERTY", "LaunchSpec"),
        new("OL_E_CONTROL_COMMAND_UNKNOWN", "LocalControl"),
        new("OL_E_CONTROL_FAILED", "LocalControl"),
        new("OL_E_CONTROL_HANDLER_FAILED", "LocalControl"),
        new("OL_E_CONTROL_MESSAGE_INVALID", "LocalControl"),
        new("OL_E_CONTROL_MESSAGE_TOO_LARGE", "LocalControl"),
        new("OL_E_CONTROL_PROTOCOL", "LocalControl"),
        new("OL_E_CONTROL_RESPONSE_TOO_LARGE", "LocalControl"),
        new("OL_E_CONTROL_SESSION_MISMATCH", "LocalControl"),
        new("OL_E_PLAN_NOT_RUNNABLE", "Other"),
        new("OL_E_ITX_PROFILE_INVALID", "Presentation"),
        new("OL_E_ITX_PROFILE_MISSING", "Presentation"),
        new("OL_E_ITX_PROFILE_REQUIRED", "Presentation"),
        new("OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH", "Presentation"),
        new("OL_E_SPLASH_ASSET_DIRECTORY_MISSING", "Presentation"),
        new("OL_E_SPLASH_ASSET_MISSING", "Presentation"),
        new("OL_E_SPLASH_FORMAT_UNSUPPORTED", "Presentation"),
        new("OL_E_PROCESS_CLEANUP_FAILED", "Process"),
        new("OL_E_PROCESS_CREATION_TIME_FAILED", "Process"),
        new("OL_E_PROCESS_EXITED_EARLY", "Process"),
        new("OL_E_PROCESS_START_FAILED", "Process"),
        new("OL_E_PROCESS_SUPERVISION", "Process"),
        new("OL_E_PROCESS_TERMINATE_FAILED", "Process"),
        new("OL_E_PROCESS_WAIT_FAILED", "Process"),
        new("OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED", "Runtime"),
        new("OL_E_DATE_TIME_APPLY_FAILED", "Runtime"),
        new("OL_E_WEATHER_INVALID", "Content"),
        new("OL_E_WEATHER_APPLY_FAILED", "Runtime"),
        new("OL_E_MAKEVEHICLE_BUS_NOT_FOUND", "Runtime"),
        new("OL_E_MAKEVEHICLE_DELTA_MULTIPLE", "Runtime"),
        new("OL_E_MAKEVEHICLE_DELTA_ZERO", "Runtime"),
        new("OL_E_MAKEVEHICLE_NATIVE_FAILED", "Runtime"),
        new("OL_E_PLACE_RANDOM_BUS_FAILED", "Runtime"),
        new("OL_E_RUNTIME_ARGUMENT_REQUIRED", "Runtime"),
        new("OL_E_RUNTIME_CURSOR_INVALID", "Runtime"),
        new("OL_E_RUNTIME_ARTIFACT_MISSING", "Runtime"),
        new("OL_E_RUNTIME_BASELINE_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_BUS_IDENTITY_INVALID", "Runtime"),
        new("OL_E_RUNTIME_CHANNEL_BUSY", "Runtime"),
        new("OL_E_RUNTIME_CHANNEL_CLOSED", "Runtime"),
        new("OL_E_RUNTIME_CHANNEL_STATE_INVALID", "Runtime"),
        new("OL_E_RUNTIME_CONSTANTS_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_CONSTANT_NOT_FOUND", "Runtime"),
        new("OL_E_RUNTIME_CREATED_OBJECT_INVALID", "Runtime"),
        new("OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION", "Runtime"),
        new("OL_E_RUNTIME_CURVE_DEGENERATE", "Runtime"),
        new("OL_E_RUNTIME_CURVE_EMPTY", "Runtime"),
        new("OL_E_RUNTIME_CURVE_INVALID", "Runtime"),
        new("OL_E_RUNTIME_CURVE_NOT_FOUND", "Runtime"),
        new("OL_E_RUNTIME_HOF_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_INSTALLATION_INCOMPLETE", "Runtime"),
        new("OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED", "Runtime"),
        new("OL_E_RUNTIME_OBJECT_HANDLE_STALE", "Runtime"),
        new("OL_E_RUNTIME_OPERATION_FAILED", "Runtime"),
        new("OL_E_RUNTIME_OPERATION_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_OPERATION_UNKNOWN", "Runtime"),
        new("OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_PROTOCOL_MISMATCH", "Runtime"),
        new("OL_E_RUNTIME_REQUEST_ID_REUSED", "Runtime"),
        new("OL_E_RUNTIME_REQUEST_TIMEOUT", "Runtime"),
        new("OL_E_RUNTIME_RESPONSE_INVALID", "Runtime"),
        new("OL_E_RUNTIME_RESPONSE_TOO_LARGE", "Runtime"),
        new("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_SESSION_MISMATCH", "Runtime"),
        new("OL_E_RUNTIME_SETTING_NOT_PERSISTENT", "Runtime"),
        new("OL_E_RUNTIME_SETTING_UNAVAILABLE", "Runtime"),
        new("OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND", "Runtime"),
        new("OL_E_RUNTIME_VALUE_INVALID", "Runtime"),
        new("OL_E_RUNTIME_VALUE_OUT_OF_RANGE", "Runtime"),
        new("OL_E_RUNTIME_VARIABLE_NOT_FOUND", "Runtime"),
        new("OL_E_RUNTIME_VARIABLE_UNAVAILABLE", "Runtime"),
        new("OL_E_TIME_APPLY_FAILED", "Runtime"),
        new("OL_E_D3D_DEVICE_LOST", "RuntimeD3D"),
        new("OL_E_D3D_INVALID_ARGUMENT", "RuntimeD3D"),
        new("OL_E_D3D_INVALID_PIXEL_BUFFER", "RuntimeD3D"),
        new("OL_E_D3D_INVALID_TEXTURE_FORMAT", "RuntimeD3D"),
        new("OL_E_D3D_NATIVE_CALL_FAILED", "RuntimeD3D"),
        new("OL_E_D3D_NOT_READY", "RuntimeD3D"),
        new("OL_E_D3D_RESET_IN_PROGRESS", "RuntimeD3D"),
        new("OL_E_D3D_RESOURCE_RELEASED", "RuntimeD3D"),
        new("OL_E_D3D_STALE_RESOURCE_HANDLE", "RuntimeD3D"),
        new("OL_E_CAPABILITY_UNAVAILABLE", "Session"),
        new("OL_E_HEADLESS_ARM_FAILED", "Session"),
        new("OL_E_NO_ACTIVE_SESSION", "Session"),
        new("OL_E_PLUGIN_NOT_LOADED", "Session"),
        new("OL_E_PLUGIN_PROTOCOL_MISMATCH", "Session"),
        new("OL_E_SESSION_ALREADY_ACTIVE", "Session"),
        new("OL_E_SESSION_NOT_RUNNING", "Session"),
        new("OL_E_SESSION_PRESENTATION_INVALID", "Session"),
        new("OL_E_SESSION_START_FAILED", "Session"),
        new("OL_E_SITUATION_LOAD_FAILED", "Session"),
        new("OL_E_STARTUP_TIMEOUT", "Session"),
        new("OL_E_START_SESSION", "Session"),
        new("OL_E_WORLD_START_FAILED", "Session"),
        new("OL_E_SESSION_PROFILE_ASSET_MISSING", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_INVALID", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_MAP_MISMATCH", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_NOT_FOUND", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_PATH_ESCAPE", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE", "SessionProfile"),
        new("OL_E_SESSION_PROFILE_SETTING_UNKNOWN", "SessionProfile"),
        new("OL_E_CLOSECHECK_REMOVE_FAILED", "Transaction"),
        new("OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH", "Transaction"),
        new("OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED", "Transaction"),
        new("OL_E_RECOVERY_BACKUP_CORRUPT", "Transaction"),
        new("OL_E_RECOVERY_JOURNAL_MISSING", "Transaction"),
        new("OL_E_RECOVERY_JOURNAL_REMOVE_FAILED", "Transaction"),
        new("OL_E_RESTORE_DEFERRED", "Transaction"),
        new("OL_E_RESTORE_FAILED", "Transaction"),
        new("OL_W_RESTORE_FOREIGN_FILE_RETAINED", "Warning"),
        new("OL_W_BUILD_UNRECOGNIZED", "Warning"),
        new("OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE", "Warning"),
        new("OL_W_CAPABILITY_PENDING", "Warning"),
        new("OL_W_MODULE_NOT_LOADED", "Warning"),
        new("OL_W_MODULE_SETTING_IGNORED", "Warning"),
        new("OL_E_MODULE_MANIFEST_INVALID", "Module"),
        new("OL_E_MODULE_NOT_FOUND", "Module"),
        new("OL_E_MODULE_INTEGRITY", "Module"),
        new("OL_E_MODULE_PROTOCOL_UNSUPPORTED", "Module"),
        new("OL_E_MODULE_CAPABILITY_UNRESOLVED", "Module"),
        new("OL_E_MODULE_CONTENT_BINDING", "Module"),
        new("OL_E_MODULE_COMPONENT_UNSUPPORTED", "Module"),
        new("OL_E_MODULE_LOAD_FAILED", "Module"),
        new("OL_E_MODULE_PHASE_FAILED", "Module"),
        new("OL_E_MODULE_PHASE_TIMEOUT", "Module"),
        new("OL_E_MODULE_DISABLED", "Module"),
        new("OL_E_MODULE_NOT_LOADED", "Module"),
        new("OL_E_MODULE_OPERATION_UNKNOWN", "Module"),
        new("OL_E_MODULE_OPERATION_FAILED", "Module"),
        new("OL_E_MODULE_CLEANUP_INCOMPLETE", "Module"),
        new("OL_E_MODULE_SESSION_FAILED", "Module"),
        new("OL_E_MODULE_HOOK_UNAVAILABLE", "Module"),
        new("OL_E_MODULE_SETTING_INVALID", "Module"),
        new("OL_E_MODULE_CONFLICT", "Module"),
        new("OL_E_MODULE_OPERATION_NOT_DECLARED", "Module"),
        new("OL_E_MODULE_CONTENT_INPUT_UNDECLARED", "Module"),
        new("OL_E_MODULE_CONTENT_INPUT_INVALID", "Module"),
        new("OL_E_UI_NOT_PAUSED", "Ui"),
        new("OL_E_UI_BUSY", "Ui"),
        new("OL_E_UI_ALREADY_TAKEN_OVER", "Ui"),
    };

    // The catalogued public code a message starts with (a code alone, or a
    // code followed by ": detail"), otherwise null. Used where a typed code travels inside an
    // exception message, so the code survives to the caller unchanged.
    public static string? FromMessage(string? message)
    {
        if (string.IsNullOrEmpty(message) || !message.StartsWith("OL_", StringComparison.Ordinal)) return null;
        var end = 0;
        while (end < message.Length && (message[end] is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')) end++;
        var code = message[..end];
        return All.Any(descriptor => descriptor.Code == code) ? code : null;
    }
}

public sealed record PublicErrorDescriptor(string Code, string Category);
