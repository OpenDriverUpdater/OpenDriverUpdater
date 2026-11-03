use serde::Deserialize;
use wmi::{WMIConnection, WMIError};
use crate::models::DeviceInfo;

#[allow(non_camel_case_types, non_snake_case)]
#[derive(Deserialize, Debug)]
struct Win32_PnPEntity {
    DeviceID: Option<String>,
    Name: Option<String>,
    Manufacturer: Option<String>,
    DriverVersion: Option<String>,
    DriverDate: Option<String>,
    HardwareID: Option<Vec<String>>,
    PNPClass: Option<String>,
    Status: Option<String>,
    ConfigManagerErrorCode: Option<u32>,
}

pub struct HardwareScanner;

impl HardwareScanner {
    pub fn scan_devices() -> Result<Vec<DeviceInfo>, String> {
        // wmi 0.14 uses COMLibrary::new() not COMLib::new()
        let com_lib = wmi::COMLibrary::new()
            .map_err(|e| format!("COM Initialization Error: {}", e))?;
        let wmi_con = WMIConnection::new(com_lib)
            .map_err(|e| format!("WMI Connection Error: {}", e))?;

        let results: Vec<Win32_PnPEntity> = wmi_con
            .raw_query("SELECT DeviceID, Name, Manufacturer, DriverVersion, DriverDate, HardwareID, PNPClass, Status, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE DeviceID IS NOT NULL AND Name IS NOT NULL")
            .map_err(|e: WMIError| format!("WMI Query Error: {}", e))?;

        let devices = results
            .into_iter()
            .filter(|pnp| {
                pnp.Name.as_deref().map(|n| !n.contains("Root")).unwrap_or(false)
            })
            .map(|pnp| {
                let err_code = pnp.ConfigManagerErrorCode.unwrap_or(0);
                DeviceInfo {
                    device_id:      pnp.DeviceID.unwrap_or_default(),
                    name:           pnp.Name.unwrap_or_else(|| "Unknown Device".to_string()),
                    manufacturer:   pnp.Manufacturer.unwrap_or_else(|| "Generic".to_string()),
                    driver_version: pnp.DriverVersion.unwrap_or_else(|| "0.0.0.0".to_string()),
                    driver_date:    pnp.DriverDate.unwrap_or_default(),
                    hardware_ids:   pnp.HardwareID.unwrap_or_default(),
                    device_class:   pnp.PNPClass.unwrap_or_else(|| "System".to_string()),
                    is_problem:     err_code != 0,
                    status:         pnp.Status.unwrap_or_else(|| "OK".to_string()),
                }
            })
            .collect();

        Ok(devices)
    }
}
