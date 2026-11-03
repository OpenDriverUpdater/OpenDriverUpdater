// ScanButton.qml — Accent scan button (FluentWinUI3 highlighted)
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3

Button {
    id: root
    required property bool scanning

    text: scanning ? "Scanning…" : "Scan Hardware"
    highlighted: !scanning
    enabled: !scanning

    contentItem: Row {
        spacing: 8
        anchors.centerIn: parent

        // Spinner visible while scanning
        BusyIndicator {
            visible: root.scanning
            width: 16; height: 16
            anchors.verticalCenter: parent.verticalCenter
            running: root.scanning
        }

        Text {
            text: root.text
            font: root.font
            color: root.palette.buttonText
            anchors.verticalCenter: parent.verticalCenter
        }
    }
}
