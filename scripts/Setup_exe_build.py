import os
import subprocess
import shutil
import sys

# Configuration
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ROOT_DIR = os.path.dirname(SCRIPT_DIR)

PROJECT_NAME = "OpenDriverUpdater"
APP_PROJECT_PATH = os.path.join(ROOT_DIR, "src", "OpenDriverUpdater.App", "OpenDriverUpdater.App.csproj")
DIST_DIR = os.path.join(ROOT_DIR, "dist")
PUBLISH_DIR = os.path.join(DIST_DIR, "publish")
CERT_PATH = os.path.join(ROOT_DIR, "certs", "OpenDriverUpdater.pfx")
APP_EXE_NAME = "OpenDriverUpdater.App.exe"
CERT_PASS = "odo85214425s-a515qs"
ISS_SCRIPT = os.path.join(ROOT_DIR, "scripts", "setup_odu.iss")

# Common tool paths
def find_tool(name, primary_path):
    if os.path.exists(primary_path): return primary_path
    path = shutil.which(name)
    if path: return path
    return primary_path

SIGNTOOL = find_tool("signtool.exe", r"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe")
ISCC = find_tool("ISCC.exe", r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe")

def run_cmd(cmd, description):
    print(f"\n--- {description} ---")
    print(f"Running: {cmd}")
    try:
        subprocess.check_call(cmd, shell=True)
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error: {description} failed.")
        return False

def fix_encoding(directory):
    print(f"Ensuring UTF-8 (No BOM) for XAML in {directory}...")
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith(".xaml"):
                path = os.path.join(root, file)
                with open(path, 'rb') as f:
                    content = f.read()
                # Remove BOM if exists
                if content.startswith(b'\xef\xbb\xbf'):
                    content = content.replace(b'\xef\xbb\xbf', b'', 1)
                with open(path, 'wb') as f:
                    f.write(content)

def main():
    # 1. Clean Dist & App Artifacts
    if os.path.exists(DIST_DIR):
        print(f"Cleaning {DIST_DIR}...")
        shutil.rmtree(DIST_DIR)
    os.makedirs(PUBLISH_DIR)

    app_dir = os.path.dirname(os.path.abspath(APP_PROJECT_PATH))
    for folder in ['bin', 'obj']:
        p = os.path.join(app_dir, folder)
        if os.path.exists(p):
            print(f"Purging {p}...")
            shutil.rmtree(p, ignore_errors=True)

    # 1.1 Fix Encoding
    fix_encoding(os.path.join(app_dir))

    # 2. Publish App (SingleFile disabled for stability)
    publish_cmd = (
        f"dotnet publish {APP_PROJECT_PATH} "
        f"-c Release -r win-x64 --self-contained true -v m "
        f"-p:PublishSingleFile=false -o {PUBLISH_DIR}"
    )
    if not run_cmd(publish_cmd, "Publishing WinUI 3 App"): sys.exit(1)

    # 3. Sign Core Binary
    app_exe = os.path.join(PUBLISH_DIR, APP_EXE_NAME)
    sign_cmd = f'"{SIGNTOOL}" sign /f "{CERT_PATH}" /p "{CERT_PASS}" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "{app_exe}"'
    if os.path.exists(SIGNTOOL):
        if not run_cmd(sign_cmd, "Signing Application Binary"): sys.exit(1)

    # 4. Compile Installer
    if os.path.exists(ISCC):
        compile_cmd = f'"{ISCC}" "{ISS_SCRIPT}"'
        if not run_cmd(compile_cmd, "Compiling Inno Setup Installer"): sys.exit(1)
    
    # 5. Sign Installer
    setup_exe = os.path.join(DIST_DIR, f"{PROJECT_NAME}_Setup.exe")
    if os.path.exists(SIGNTOOL) and os.path.exists(setup_exe):
        sign_setup_cmd = f'"{SIGNTOOL}" sign /f "{CERT_PATH}" /p "{CERT_PASS}" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "{setup_exe}"'
        if not run_cmd(sign_setup_cmd, "Signing Installer"): sys.exit(1)

    print(f"\n[SUCCESS] Build Complete! Your signed installer is at: {setup_exe}")

if __name__ == "__main__":
    main()
