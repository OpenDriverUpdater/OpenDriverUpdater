use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use crate::models::DeviceInfo;
use crate::scanner::HardwareScanner;
use crate::resolver::DriverResolver;
use crate::catalog::CatalogSource;

#[no_mangle]
pub extern "C" fn odu_scan_hardware_json() -> *mut c_char {
    match HardwareScanner::scan_devices() {
        Ok(devices) => {
            let json = serde_json::to_string(&devices).unwrap_or_else(|_| "[]".to_string());
            CString::new(json).unwrap().into_raw()
        }
        Err(err) => {
            let err_msg = format!("{{\"error\": \"{}\"}}", err.replace('"', "\\\""));
            CString::new(err_msg).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub extern "C" fn odu_check_updates_json(device_json_ptr: *const c_char) -> *mut c_char {
    if device_json_ptr.is_null() {
        return CString::new("[]").unwrap().into_raw();
    }

    let c_str = unsafe { CStr::from_ptr(device_json_ptr) };
    let json_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return CString::new("[]").unwrap().into_raw(),
    };

    let device: DeviceInfo = match serde_json::from_str(json_str) {
        Ok(d) => d,
        Err(_) => return CString::new("[]").unwrap().into_raw(),
    };

    let rt = match tokio::runtime::Runtime::new() {
        Ok(r) => r,
        Err(_) => return CString::new("[]").unwrap().into_raw(),
    };

    let recommendations = rt.block_on(async {
        match CatalogSource::search_updates(&device).await {
            Ok(candidates) => {
                candidates
                    .into_iter()
                    .map(|cand| DriverResolver::evaluate_candidate(&device, &cand))
                    .collect::<Vec<_>>()
            }
            Err(_) => Vec::new(),
        }
    });

    let result_json = serde_json::to_string(&recommendations).unwrap_or_else(|_| "[]".to_string());
    CString::new(result_json).unwrap().into_raw()
}

#[no_mangle]
pub extern "C" fn odu_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}
