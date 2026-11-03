use crate::models::{CandidateDriver, DeviceInfo, TrustScore, UpdateRecommendation};

pub struct DriverResolver;

impl DriverResolver {
    pub fn compare_versions(v1: &str, v2: &str) -> i32 {
        let parse = |v: &str| -> Vec<u64> {
            v.split(|c: char| !c.is_numeric())
                .filter_map(|s| s.parse::<u64>().ok())
                .collect()
        };

        let nums1 = parse(v1);
        let nums2 = parse(v2);

        let max_len = usize::max(nums1.len(), nums2.len());
        for i in 0..max_len {
            let n1 = nums1.get(i).copied().unwrap_or(0);
            let n2 = nums2.get(i).copied().unwrap_or(0);
            if n1 > n2 {
                return 1;
            }
            if n1 < n2 {
                return -1;
            }
        }
        0
    }

    pub fn evaluate_trust(device: &DeviceInfo, candidate: &CandidateDriver) -> TrustScore {
        let mut score = 50;
        let mut breakdown = Vec::new();

        let is_official = candidate.source.to_lowercase().contains("microsoft") 
            || candidate.source.to_lowercase().contains("intel")
            || candidate.source.to_lowercase().contains("amd")
            || candidate.source.to_lowercase().contains("nvidia");

        if is_official {
            score += 30;
            breakdown.push("Official source verified (+30)".to_string());
        }

        let matches_hw = candidate.hardware_ids.iter().any(|cid| {
            device.hardware_ids.iter().any(|did| did.eq_ignore_ascii_case(cid))
        });

        if matches_hw {
            score += 20;
            breakdown.push("Hardware ID exact match (+20)".to_string());
        } else {
            score -= 15;
            breakdown.push("Hardware ID match unverified (-15)".to_string());
        }

        TrustScore {
            total_score: score.clamp(0, 100),
            is_official_source: is_official,
            is_signed: true,
            matches_hardware_id: matches_hw,
            score_breakdown: breakdown,
        }
    }

    pub fn evaluate_candidate(device: &DeviceInfo, candidate: &CandidateDriver) -> UpdateRecommendation {
        let cmp = Self::compare_versions(&candidate.version, &device.driver_version);
        let is_newer = cmp > 0;
        let trust_score = Self::evaluate_trust(device, candidate);

        let level = if is_newer && trust_score.total_score >= 80 {
            "RECOMMENDED".to_string()
        } else if is_newer && trust_score.total_score >= 50 {
            "OPTIONAL".to_string()
        } else {
            "NOT_RECOMMENDED".to_string()
        };

        UpdateRecommendation {
            device: device.clone(),
            candidate: candidate.clone(),
            trust_score,
            is_newer,
            recommendation_level: level,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_version_compare() {
        assert_eq!(DriverResolver::compare_versions("31.0.101.4514", "30.0.101.2000"), 1);
        assert_eq!(DriverResolver::compare_versions("10.0.1.1", "10.0.1.1"), 0);
        assert_eq!(DriverResolver::compare_versions("1.0.0", "2.0.0"), -1);
    }
}
