// StatusBar.qml — Animated status message strip
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3

Rectangle {
    id: root

    required property bool   scanning
    required property int    deviceCount
    required property string filterInfo

    height: 36
    color: Qt.rgba(0.04, 0.05, 0.09, 1)

    // Top separator
    Rectangle {
        anchors.top: parent.top
        width: parent.width; height: 1
        color: Qt.rgba(1,1,1,0.05)
    }

    Row {
        anchors { left: parent.left; right: parent.right; verticalCenter: parent.verticalCenter }
        anchors.leftMargin: 20; anchors.rightMargin: 20
        spacing: 16

        // Animated status indicator dot
        Rectangle {
            width: 7; height: 7; radius: 3.5
            anchors.verticalCenter: parent.verticalCenter
            color: root.scanning ? "#00B4D8" : (root.deviceCount > 0 ? "#00E676" : "#4A5568")

            SequentialAnimation on opacity {
                running: root.scanning
                loops: Animation.Infinite
                NumberAnimation { to: 0.2; duration: 600 }
                NumberAnimation { to: 1.0; duration: 600 }
            }
        }

        Label {
            anchors.verticalCenter: parent.verticalCenter
            text: root.scanning
                  ? "Interrogating WMI endpoints via Rust core…"
                  : (root.deviceCount > 0
                     ? "Scan complete — " + root.deviceCount + " hardware nodes detected. " + root.filterInfo
                     : "Select 'Scan Hardware' to begin diagnostic")
            font.pixelSize: 12
            color: root.scanning ? "#00B4D8" : Qt.rgba(0.65, 0.72, 0.88, 1)

            Behavior on text { }
        }
    }
}
