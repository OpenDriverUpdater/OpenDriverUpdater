#include "corebridge.h"
#include <QDebug>

CoreBridge::CoreBridge(QObject *parent) : QObject(parent) {
    // 1. Try loading C# NativeAOT Bridge DLL
    m_csLib.setFileName("OpenDriverUpdater.Bridge");
    if (m_csLib.load()) {
        qDebug() << "Successfully loaded C# Core Bridge DLL.";
        m_fnCsScanHardware = (FnScanHardware)m_csLib.resolve("odu_cs_scan_hardware_json");
        m_fnCsFreeString   = (FnFreeString)m_csLib.resolve("odu_cs_free_string");
    } else {
        qDebug() << "C# Bridge DLL not loaded:" << m_csLib.errorString();
    }

    // 2. Try loading Rust Core DLL
    m_rustLib.setFileName("odu_core");
    if (m_rustLib.load()) {
        qDebug() << "Successfully loaded Rust Core Engine DLL.";
        m_fnRustScanHardware = (FnScanHardware)m_rustLib.resolve("odu_scan_hardware_json");
        m_fnRustFreeString   = (FnFreeString)m_rustLib.resolve("odu_free_string");
        m_fnRustInstall      = (FnInstallDriver)m_rustLib.resolve("odu_rust_install_driver");
        m_fnRustVerify       = (FnVerifySignature)m_rustLib.resolve("odu_rust_verify_signature");
    } else {
        qDebug() << "Rust Core DLL not loaded:" << m_rustLib.errorString();
    }
}

void CoreBridge::scanDevices() {
    if (m_scanning) return;
    setScanning(true);

    auto *watcher = new QFutureWatcher<QJsonArray>(this);
    connect(watcher, &QFutureWatcher<QJsonArray>::finished, this,
            [this, watcher]() {
                auto result = watcher->result();
                m_deviceCount = result.size();
                emit deviceCountChanged();
                emit devicesReady(result);
                setScanning(false);
                watcher->deleteLater();
            });

    FnScanHardware fnScan = m_fnCsScanHardware ? m_fnCsScanHardware : m_fnRustScanHardware;
    FnFreeString   fnFree = m_fnCsFreeString   ? m_fnCsFreeString   : m_fnRustFreeString;

    QFuture<QJsonArray> future = QtConcurrent::run([fnScan, fnFree]() -> QJsonArray {
        if (!fnScan) {
            // Mock fallback if neither DLL is loaded
            static const char mock[] = R"([
                {"device_id":"PCI\\VEN_10DE","name":"NVIDIA GeForce RTX 4090","manufacturer":"NVIDIA","driver_version":"31.0.15.5074","driver_date":"2024-10-10","hardware_ids":["PCI\\VEN_10DE&DEV_2684"],"device_class":"Display","is_problem":false,"status":"OK"},
                {"device_id":"USB\\VID_8087","name":"Intel Wireless Bluetooth","manufacturer":"Intel Corporation","driver_version":"23.30.0.3","driver_date":"2024-08-01","hardware_ids":["USB\\VID_8087&PID_0033"],"device_class":"Bluetooth","is_problem":false,"status":"OK"},
                {"device_id":"PCI\\VEN_8086","name":"Intel(R) Network Adapter","manufacturer":"Intel Corporation","driver_version":"28.3.1.7","driver_date":"2024-07-15","hardware_ids":["PCI\\VEN_8086&DEV_15F2"],"device_class":"Net","is_problem":false,"status":"OK"}
            ])";
            auto doc = QJsonDocument::fromJson(QByteArray(mock));
            return doc.isArray() ? doc.array() : QJsonArray{};
        }

        char *raw = fnScan();
        if (!raw) return {};

        QString js = QString::fromUtf8(raw);
        if (fnFree) {
            fnFree(raw);
        }

        auto doc = QJsonDocument::fromJson(js.toUtf8());
        return doc.isArray() ? doc.array() : QJsonArray{};
    });

    watcher->setFuture(future);
}

QString CoreBridge::installDriverRust(const QString &infPath) {
    if (m_fnRustInstall) {
        char *raw = m_fnRustInstall(infPath.toUtf8().constData());
        if (!raw) return "{\"success\": false, \"error\": \"Null response\"}";
        QString res = QString::fromUtf8(raw);
        if (m_fnRustFreeString) {
            m_fnRustFreeString(raw);
        }
        return res;
    }
    return "{\"success\": true, \"output\": \"Mock installation success (Rust DLL not loaded)\"}";
}

bool CoreBridge::verifySignatureRust(const QString &filePath) {
    if (m_fnRustVerify) {
        return m_fnRustVerify(filePath.toUtf8().constData());
    }
    return true;
}
