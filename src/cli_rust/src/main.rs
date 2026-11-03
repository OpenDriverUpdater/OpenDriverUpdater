use clap::{Parser, Subcommand};
use odu_core::{HardwareScanner, CatalogSource, DriverResolver};

#[derive(Parser)]
#[command(name = "odu")]
#[command(about = "OpenDriverUpdater CLI - High Performance Rust Engine", long_about = None)]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Scan local system hardware devices
    Scan,
    /// List detected hardware devices with details
    ListDevices,
    /// Check Microsoft Update Catalog for driver updates
    CheckUpdates,
}

#[tokio::main]
async fn main() {
    let cli = Cli::parse();

    match cli.command {
        Commands::Scan | Commands::ListDevices => {
            println!("🔍 Interrogating WMI endpoints...");
            match HardwareScanner::scan_devices() {
                Ok(devices) => {
                    println!("✅ Scan Complete! Found {} devices.\n", devices.len());
                    for (idx, dev) in devices.iter().enumerate() {
                        println!(
                            "[{:03}] {} | Class: {} | Ver: {} | Status: {}",
                            idx + 1,
                            dev.name,
                            dev.device_class,
                            dev.driver_version,
                            if dev.is_problem { "⚠️ WARNING" } else { "✓ OK" }
                        );
                    }
                }
                Err(e) => {
                    eprintln!("❌ Scan error: {}", e);
                }
            }
        }
        Commands::CheckUpdates => {
            println!("🌐 Checking Microsoft Update Catalog for updates...");
            match HardwareScanner::scan_devices() {
                Ok(devices) => {
                    for dev in devices.iter().take(5) {
                        println!("Checking updates for: {}", dev.name);
                        if let Ok(candidates) = CatalogSource::search_updates(dev).await {
                            for cand in candidates {
                                let rec = DriverResolver::evaluate_candidate(dev, &cand);
                                println!(
                                    "  -> Candidate: {} [Ver: {}] | Trust: {}/100 ({})",
                                    cand.title,
                                    cand.version,
                                    rec.trust_score.total_score,
                                    rec.recommendation_level
                                );
                            }
                        }
                    }
                }
                Err(e) => eprintln!("Error: {}", e),
            }
        }
    }
}
