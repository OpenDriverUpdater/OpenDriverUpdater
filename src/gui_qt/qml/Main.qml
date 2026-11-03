// Main.qml — OpenDriverUpdater FluentWinUI3 Root Window
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3
import QtQuick.Layouts
import OpenDriverUpdater

ApplicationWindow {
    id: root
    visible: true
    width: 1200
    height: 780
    minimumWidth: 900
    minimumHeight: 600
    title: "OpenDriverUpdater v2.0"

    // ── Rust core bridge ──────────────────────────────────────────────────
    CoreBridge {
        id: bridge
        onDevicesReady: function(devices) {
            deviceModel.clear()
            for (let i = 0; i < devices.length; i++) {
                const d = devices[i]
                deviceModel.append({
                    name:          d.name         ?? "Unknown",
                    deviceClass:   d.device_class ?? "System",
                    manufacturer:  d.manufacturer ?? "Generic",
                    driverVersion: d.driver_version ?? "0.0.0.0",
                    driverDate:    d.driver_date  ?? "",
                    isProblem:     d.is_problem   ?? false,
                    status:        d.status       ?? "OK",
                    hardwareId:    (d.hardware_ids ?? [])[0] ?? ""
                })
            }
        }
    }

    ListModel { id: deviceModel }

    // ── Layout ────────────────────────────────────────────────────────────
    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 0
        spacing: 0

        // Header bar
        Rectangle {
            id: headerBar
            Layout.fillWidth: true
            height: 72
            color: Qt.rgba(0.06, 0.07, 0.12, 1)

            // bottom accent line
            Rectangle {
                anchors.bottom: parent.bottom
                width: parent.width
                height: 1
                color: Qt.rgba(0, 0.75, 1, 0.18)
            }

            RowLayout {
                anchors {
                    left: parent.left; right: parent.right
                    verticalCenter: parent.verticalCenter
                    leftMargin: 28; rightMargin: 20
                }
                spacing: 12

                // App logo column
                Column {
                    spacing: 2
                    Label {
                        text: "OpenDriverUpdater"
                        font { pixelSize: 20; bold: true; letterSpacing: 0.6 }
                        color: "#FFFFFF"
                    }
                    Label {
                        text: "Qt 6.8 · FluentWinUI3 · Rust Core Engine"
                        font { pixelSize: 10; letterSpacing: 0.4 }
                        color: "#00B4D8"
                    }
                }

                Item { Layout.fillWidth: true }

                // Node badge
                Rectangle {
                    visible: deviceModel.count > 0
                    width: nodeLabel.implicitWidth + 20
                    height: 28
                    radius: 14
                    color: Qt.rgba(0, 0.72, 1, 0.12)
                    border.color: Qt.rgba(0, 0.72, 1, 0.35)
                    Label {
                        id: nodeLabel
                        anchors.centerIn: parent
                        text: deviceModel.count + " devices"
                        font { pixelSize: 11; bold: true }
                        color: "#00B4D8"
                    }
                }

                // Scan button — uses FluentWinUI3 Button
                Button {
                    id: scanBtn
                    text: bridge.scanning ? "Scanning…" : "Scan Hardware"
                    enabled: !bridge.scanning
                    icon.name: bridge.scanning ? "" : "view-refresh"
                    onClicked: bridge.scanDevices()

                    // FluentWinUI3 accent style override
                    highlighted: true
                }
            }
        }

        // ── Progress / status strip ───────────────────────────────────────
        Rectangle {
            Layout.fillWidth: true
            height: 3
            color: "transparent"

            ProgressBar {
                anchors.fill: parent
                indeterminate: true
                visible: bridge.scanning
                // FluentWinUI3 provides its own Fluent-styled progressbar
            }
        }

        // ── Filter / search bar ───────────────────────────────────────────
        Rectangle {
            Layout.fillWidth: true
            height: 52
            color: Qt.rgba(0.05, 0.06, 0.1, 1)

            RowLayout {
                anchors {
                    fill: parent
                    leftMargin: 20; rightMargin: 20
                    topMargin: 8; bottomMargin: 8
                }
                spacing: 12

                TextField {
                    id: searchField
                    Layout.preferredWidth: 280
                    placeholderText: "Filter devices…"
                    // FluentWinUI3 TextField automatically styled
                }

                ComboBox {
                    id: classFilter
                    model: ["All Classes", "Display", "Net", "MEDIA", "Bluetooth", "HIDClass", "System"]
                    // FluentWinUI3 ComboBox
                }

                CheckBox {
                    id: problemOnly
                    text: "Problems only"
                }

                Item { Layout.fillWidth: true }

                Label {
                    text: bridge.scanning
                          ? "Interrogating WMI endpoints via Rust core…"
                          : (deviceModel.count > 0
                             ? "Scan complete — " + deviceModel.count + " hardware nodes detected"
                             : "Select 'Scan Hardware' to begin diagnostic")
                    color: bridge.scanning ? "#00B4D8" : Qt.rgba(0.7, 0.75, 0.85, 1)
                    font.pixelSize: 12
                }
            }
        }

        // ── Header row ───────────────────────────────────────────────────
        DeviceTableView {
            id: tableView
            Layout.fillWidth: true
            Layout.fillHeight: true
            model: deviceModel
            searchText: searchField.text
            classFilterText: classFilter.currentText
            showProblemsOnly: problemOnly.checked
        }

        // ── Footer ───────────────────────────────────────────────────────
        Rectangle {
            Layout.fillWidth: true
            height: 32
            color: Qt.rgba(0.04, 0.05, 0.09, 1)

            RowLayout {
                anchors {
                    fill: parent
                    leftMargin: 20; rightMargin: 20
                }
                Label {
                    text: "OpenDriverUpdater v2.0  •  MIT License  •  Rust " +
                          (typeof rustVersion !== "undefined" ? rustVersion : "1.93")
                    font.pixelSize: 10
                    color: Qt.rgba(0.4, 0.45, 0.55, 1)
                }
                Item { Layout.fillWidth: true }
                Label {
                    text: "Qt 6.8.1 FluentWinUI3"
                    font.pixelSize: 10
                    color: Qt.rgba(0.4, 0.45, 0.55, 1)
                }
            }
        }
    }
}
