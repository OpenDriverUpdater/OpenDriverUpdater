use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DeviceInfo {
    pub device_id: String,
    pub name: String,
    pub manufacturer: String,
    pub driver_version: String,
    pub driver_date: String,
    pub hardware_ids: Vec<String>,
    pub device_class: String,
    pub is_problem: bool,
    pub status: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CandidateDriver {
    pub id: String,
    pub title: String,
    pub version: String,
    pub release_date: String,
    pub provider: String,
    pub download_url: String,
    pub file_size: u64,
    pub hardware_ids: Vec<String>,
    pub source: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TrustScore {
    pub total_score: i32,
    pub is_official_source: bool,
    pub is_signed: bool,
    pub matches_hardware_id: bool,
    pub score_breakdown: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateRecommendation {
    pub device: DeviceInfo,
    pub candidate: CandidateDriver,
    pub trust_score: TrustScore,
    pub is_newer: bool,
    pub recommendation_level: String,
}
