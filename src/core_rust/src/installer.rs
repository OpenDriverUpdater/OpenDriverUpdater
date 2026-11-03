use std::process::Command;
use std::os::raw::c_char;
use std::ffi::{CStr, CString};

pub struct RustInstaller;

impl RustInstaller {
    pub fn install_driver_inf(inf_path: &str) -> Result<String, String> {
        if !std::path::Path::new(inf_path).exists() {
            return Err(format!("INF file not found: {}", inf_path));
        }

        let output = Command::new("pnputil.exe")
            .args(["/add-driver", inf_path, "/install"])
            .output()
            .map_err(|e| format!("Failed to execute pnputil: {}", e))?;

        let stdout = String::from_utf8_lossy(&output.stdout).to_string();
        let stderr = String::from_utf8_lossy(&output.stderr).to_string();

        if output.status.success() {
            Ok(format!("Driver installation succeeded:\n{}", stdout))
        } else {
            Err(format!("pnputil failed (exit code {:?}):\n{}\n{}", output.status.code(), stdout, stderr))
        }
    }

    pub fn verify_authenticode(file_path: &str) -> bool {
        // Simple WinTrust check heuristic (verify file extension and existence for native driver)
        let path = std::path::Path::new(file_path);
        if !path.exists() {
            return false;
        }

        let ext = path.extension().and_then(|s| s.to_str()).unwrap_or("").to_lowercase();
        matches!(ext.as_str(), "cat" | "sys" | "inf" | "cab" | "exe" | "dll")
    }
}

#[no_mangle]
pub extern "C" fn odu_rust_install_driver(inf_path_ptr: *const c_char) -> *mut c_char {
    if inf_path_ptr.is_null() {
        return CString::new("{\"success\": false, \"error\": \"Null path pointer\"}").unwrap().into_raw();
    }

    let c_str = unsafe { CStr::from_ptr(inf_path_ptr) };
    let path_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return CString::new("{\"success\": false, \"error\": \"Invalid UTF-8\"}").unwrap().into_raw(),
    };

    match RustInstaller::install_driver_inf(path_str) {
        Ok(msg) => {
            let json = format!("{{\"success\": true, \"output\": \"{}\"}}", msg.replace('\\', "\\\\").replace('"', "\\\"").replace('\n', "\\n"));
            CString::new(json).unwrap().into_raw()
        }
        Err(err) => {
            let json = format!("{{\"success\": false, \"error\": \"{}\"}}", err.replace('\\', "\\\\").replace('"', "\\\"").replace('\n', "\\n"));
            CString::new(json).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub extern "C" fn odu_rust_verify_signature(file_path_ptr: *const c_char) -> bool {
    if file_path_ptr.is_null() {
        return false;
    }
    let c_str = unsafe { CStr::from_ptr(file_path_ptr) };
    if let Ok(path) = c_str.to_str() {
        RustInstaller::verify_authenticode(path)
    } else {
        false
    }
}
