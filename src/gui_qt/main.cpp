// main.cpp — Qt Quick + FluentWinUI3 entry point
#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQuickStyle>
#include <QIcon>
#include "corebridge.h"

int main(int argc, char *argv[])
{
    QGuiApplication app(argc, argv);
    app.setApplicationName("OpenDriverUpdater");
    app.setApplicationVersion("2.0.0");
    app.setOrganizationName("OpenDriverUpdater");

    // App icon — embedded via qt_add_resources at prefix "/"
    // Path: qrc:/../../img/odu_favicon.ico
    app.setWindowIcon(QIcon(":/../../img/odu_favicon.ico"));

    // ── FluentWinUI3 style (Qt 6.8) ───────────────────────────────────────
    QQuickStyle::setStyle("FluentWinUI3");

    // Register CoreBridge as a QML type
    qmlRegisterType<CoreBridge>("OpenDriverUpdater", 1, 0, "CoreBridge");

    QQmlApplicationEngine engine;

    // QRC path: RESOURCE_PREFIX "/" + URI "OpenDriverUpdater" + file "qml/Main.qml"
    // → qrc:/OpenDriverUpdater/qml/Main.qml
    const QUrl url(u"qrc:/OpenDriverUpdater/qml/Main.qml"_qs);

    QObject::connect(
        &engine,
        &QQmlApplicationEngine::objectCreationFailed,
        &app,
        []() { QCoreApplication::exit(-1); },
        Qt::QueuedConnection
    );

    engine.load(url);

    return QGuiApplication::exec();
}
