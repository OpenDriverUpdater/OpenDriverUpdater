// UpdatesView.qml — Driver Updates & Rust Installer Interface
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3
import QtQuick.Layouts

Item {
    id: root

    required property var recommendationModel
    signal installRequested(string infPath, string title)

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 20
        spacing: 16

        // Title & status banner
        Rectangle {
            Layout.fillWidth: true
            height: 60
            color: Qt.rgba(0.06, 0.08, 0.14, 1)
            radius: 8
            border.color: Qt.rgba(0, 0.72, 1, 0.2)

            RowLayout {
                anchors { fill: parent; leftMargin: 16; rightMargin: 16 }
                Column {
                    Label {
                        text: "AVAILABLE DRIVER UPDATES"
                        font { pixelSize: 15; bold: true }
                        color: "#FFFFFF"
                    }
                    Label {
                        text: "Powered by OpenDriverUpdater C# Resolver & Rust Security Engine"
                        font.pixelSize: 11
                        color: "#00B4D8"
                    }
                }
                Item { Layout.fillWidth: true }
                Button {
                    text: "Install All Recommended"
                    highlighted: true
                    onClicked: {
                        // Trigger batch update installation
                    }
                }
            }
        }

        // List of update recommendations
        ScrollView {
            Layout.fillWidth: true
            Layout.fillHeight: true

            ListView {
                id: listView
                model: recommendationModel
                spacing: 10
                clip: true

                delegate: Rectangle {
                    width: listView.width
                    height: 80
                    color: Qt.rgba(0.07, 0.08, 0.13, 1)
                    radius: 8
                    border.color: modelData.trustScore >= 80 ? Qt.rgba(0, 0.9, 0.4, 0.3) : Qt.rgba(1, 0.7, 0, 0.3)

                    RowLayout {
                        anchors { fill: parent; leftMargin: 16; rightMargin: 16 }
                        spacing: 16

                        Column {
                            Layout.fillWidth: true
                            spacing: 4
                            Label {
                                text: modelData.title || "Driver Update Package"
                                font { pixelSize: 13; bold: true }
                                color: "#E8EAF2"
                            }
                            Label {
                                text: "Version: " + modelData.version + "  •  Source: " + modelData.provider
                                font.pixelSize: 11
                                color: "#90A4AE"
                            }
                        }

                        // Trust score badge
                        Rectangle {
                            width: 90; height: 28; radius: 14
                            color: modelData.trustScore >= 80 ? Qt.rgba(0, 0.8, 0.4, 0.15) : Qt.rgba(1, 0.6, 0, 0.15)
                            border.color: modelData.trustScore >= 80 ? "#00E676" : "#FFA000"
                            Label {
                                anchors.centerIn: parent
                                text: "Trust: " + modelData.trustScore + "/100"
                                font { pixelSize: 10; bold: true }
                                color: modelData.trustScore >= 80 ? "#69F0AE" : "#FFD54F"
                            }
                        }

                        Button {
                            text: "Install Update"
                            highlighted: true
                            onClicked: {
                                root.installRequested(modelData.infPath || "", modelData.title || "")
                            }
                        }
                    }
                }
            }
        }
    }
}
