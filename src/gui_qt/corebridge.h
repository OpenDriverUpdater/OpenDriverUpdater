#ifndef COREBRIDGE_H
#define COREBRIDGE_H

#include <QObject>
#include <QString>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QLibrary>
#include <QtConcurrent>
#include <QFutureWatcher>
#include <qqml.h>

typedef char* (*FnScanHardware)();
typedef void  (*FnFreeString)(char*);
typedef char* (*FnInstallDriver)(const char*);
typedef bool  (*FnVerifySignature)(const char*);

class CoreBridge : public QObject
{
    Q_OBJECT
    QML_ELEMENT

    Q_PROPERTY(bool scanning READ scanning NOTIFY scanningChanged)
    Q_PROPERTY(int  deviceCount READ deviceCount NOTIFY deviceCountChanged)

public:
    explicit CoreBridge(QObject *parent = nullptr);
    ~CoreBridge() override = default;

    bool scanning()    const { return m_scanning; }
    int  deviceCount() const { return m_deviceCount; }

    Q_INVOKABLE void scanDevices();
    Q_INVOKABLE QString installDriverRust(const QString &infPath);
    Q_INVOKABLE bool verifySignatureRust(const QString &filePath);

signals:
    void devicesReady(const QJsonArray &devices);
    void scanningChanged();
    void deviceCountChanged();

private:
    void setScanning(bool v) { if (m_scanning == v) return; m_scanning = v; emit scanningChanged(); }
    
    QLibrary m_csLib;
    QLibrary m_rustLib;

    FnScanHardware     m_fnCsScanHardware = nullptr;
    FnFreeString       m_fnCsFreeString   = nullptr;

    FnScanHardware     m_fnRustScanHardware = nullptr;
    FnFreeString       m_fnRustFreeString   = nullptr;
    FnInstallDriver    m_fnRustInstall      = nullptr;
    FnVerifySignature  m_fnRustVerify       = nullptr;

    bool m_scanning     = false;
    int  m_deviceCount  = 0;
};

#endif // COREBRIDGE_H
