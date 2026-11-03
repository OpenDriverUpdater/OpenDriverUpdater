// DeviceCard.qml — Individual hardware device row
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3
import QtQuick.Layouts

Item {
    id: card

    required property string deviceName
    required property string deviceClass
    required property string manufacturer
    required property string driverVersion
    required property string driverDate
    required property bool   isProblem
    required property string status
    required property string hardwareId
    required property int    index

    property bool expanded: false

    height: expanded ? 108 : 52
    Behavior on height { NumberAnimation { duration: 180; easing.type: Easing.InOutQuad } }

    // ── Row background ────────────────────────────────────────────────────
    Rectangle {
        anchors.fill: parent
        color: hoverArea.containsMouse
               ? Qt.rgba(0, 0.55, 0.9, 0.09)
               : (index % 2 === 0
                  ? Qt.rgba(0.055, 0.065, 0.11, 1)
                  : Qt.rgba(0.06, 0.07, 0.12, 1))
        Behavior on color { ColorAnimation { duration: 100 } }

        // left accent stripe for problem devices
        Rectangle {
            visible: card.isProblem
            width: 3; height: parent.height
            anchors.left: parent.left
            color: "#FF4D4D"
        }

        // bottom separator
        Rectangle {
            anchors.bottom: parent.bottom; width: parent.width; height: 1
            color: Qt.rgba(1,1,1,0.04)
        }

        // ── Main row ──────────────────────────────────────────────────────
        Row {
            id: mainRow
            anchors { top: parent.top; left: parent.left; right: parent.right }
            anchors.leftMargin: 12
            height: 52

            // Device Name (flex 3)
            Item {
                width: (card.width - 12) * 3 / 9.5
                height: 52
                Column {
                    anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                    spacing: 2
                    Label {
                        text: card.deviceName
                        font { pixelSize: 13; bold: card.isProblem }
                        color: card.isProblem ? "#FF6B6B" : "#E8EAF2"
                        elide: Text.ElideRight
                        width: parent.parent.width - 8
                    }
                    Label {
                        visible: card.hardwareId !== ""
                        text: card.hardwareId
                        font.pixelSize: 10
                        color: Qt.rgba(0.45, 0.5, 0.65, 1)
                        elide: Text.ElideRight
                        width: parent.parent.width - 8
                    }
                }
            }

            // Device Class (flex 1)
            Item {
                width: (card.width - 12) * 1 / 9.5
                height: 52
                Rectangle {
                    anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                    width: classLabel.implicitWidth + 14
                    height: 22
                    radius: 11
                    color: {
                        switch(card.deviceClass) {
                            case "Display":   return Qt.rgba(0.33, 0, 0.9, 0.18)
                            case "Net":       return Qt.rgba(0, 0.6, 0.4, 0.18)
                            case "MEDIA":     return Qt.rgba(0.9, 0.4, 0, 0.18)
                            case "Bluetooth": return Qt.rgba(0, 0.35, 1, 0.18)
                            default:          return Qt.rgba(0.3, 0.35, 0.5, 0.15)
                        }
                    }
                    Label {
                        id: classLabel
                        anchors.centerIn: parent
                        text: card.deviceClass
                        font { pixelSize: 10; bold: true }
                        color: {
                            switch(card.deviceClass) {
                                case "Display":   return "#BB86FC"
                                case "Net":       return "#00E676"
                                case "MEDIA":     return "#FFA040"
                                case "Bluetooth": return "#448AFF"
                                default:          return "#90A4AE"
                            }
                        }
                    }
                }
            }

            // Manufacturer (flex 2)
            Item {
                width: (card.width - 12) * 2 / 9.5
                height: 52
                Label {
                    anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                    text: card.manufacturer
                    font.pixelSize: 12
                    color: Qt.rgba(0.7, 0.75, 0.88, 1)
                    elide: Text.ElideRight
                    width: parent.width - 8
                }
            }

            // Driver Version (flex 1.5)
            Item {
                width: (card.width - 12) * 1.5 / 9.5
                height: 52
                Column {
                    anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                    spacing: 2
                    Label {
                        text: card.driverVersion
                        font { pixelSize: 12; family: "Consolas" }
                        color: Qt.rgba(0.7, 0.75, 0.88, 1)
                    }
                    Label {
                        visible: card.driverDate !== ""
                        text: card.driverDate.substring(0, 10)
                        font.pixelSize: 10
                        color: Qt.rgba(0.4, 0.45, 0.6, 1)
                    }
                }
            }

            // Status (flex 1)
            Item {
                width: (card.width - 12) * 1 / 9.5
                height: 52
                Row {
                    anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                    spacing: 6
                    Rectangle {
                        width: 8; height: 8; radius: 4
                        anchors.verticalCenter: parent.verticalCenter
                        color: card.isProblem ? "#FF4D4D" : "#00E676"
                    }
                    Label {
                        text: card.isProblem ? "Warning" : "OK"
                        font { pixelSize: 12; bold: card.isProblem }
                        color: card.isProblem ? "#FF6B6B" : "#69F0AE"
                    }
                }
            }
        }

        // ── Expanded detail panel ─────────────────────────────────────────
        Rectangle {
            anchors {
                top: mainRow.bottom; left: parent.left; right: parent.right
                leftMargin: 12; rightMargin: 12
            }
            height: 56
            visible: card.expanded
            opacity: card.expanded ? 1 : 0
            Behavior on opacity { NumberAnimation { duration: 160 } }
            color: Qt.rgba(0, 0.4, 0.8, 0.07)
            radius: 6

            RowLayout {
                anchors { fill: parent; margins: 12 }
                spacing: 28
                Column {
                    spacing: 2
                    Label { text: "HARDWARE ID"; font.pixelSize: 9; color: Qt.rgba(0.5,0.55,0.7,1) }
                    Label {
                        text: card.hardwareId !== "" ? card.hardwareId : "N/A"
                        font.pixelSize: 11
                        font.family: "Consolas"
                        color: "#A0B0CC"
                    }
                }
                Column {
                    spacing: 2
                    Label { text: "DRIVER DATE"; font.pixelSize: 9; color: Qt.rgba(0.5,0.55,0.7,1) }
                    Label { text: card.driverDate.substring(0, 10) || "N/A"; font.pixelSize: 11; color: "#A0B0CC" }
                }
                Item { Layout.fillWidth: true }
                Button {
                    text: "Check Updates"
                    highlighted: false
                    font.pixelSize: 11
                    // Future: invoke bridge.checkUpdates(hardwareId)
                }
            }
        }

        // ── Hover / click area ────────────────────────────────────────────
        MouseArea {
            id: hoverArea
            anchors.fill: parent
            hoverEnabled: true
            cursorShape: Qt.PointingHandCursor
            onClicked: card.expanded = !card.expanded
        }
    }
}
