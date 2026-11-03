// DeviceTableView.qml — Hardware device list using FluentWinUI3 TableView
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.FluentWinUI3
import QtQuick.Layouts

Item {
    id: root

    required property var   model
    required property string searchText
    required property string classFilterText
    required property bool   showProblemsOnly

    // ── Proxy / filter ────────────────────────────────────────────────────
    property var filteredModel: {
        const arr = []
        const q = searchText.toLowerCase()
        for (let i = 0; i < model.count; i++) {
            const d = model.get(i)
            if (showProblemsOnly && !d.isProblem) continue
            if (classFilterText !== "All Classes" && d.deviceClass !== classFilterText) continue
            if (q && !d.name.toLowerCase().includes(q)
                   && !d.manufacturer.toLowerCase().includes(q)
                   && !d.hardwareId.toLowerCase().includes(q)) continue
            arr.push(d)
        }
        return arr
    }

    // ── Column header row ─────────────────────────────────────────────────
    Rectangle {
        id: columnHeader
        anchors { top: parent.top; left: parent.left; right: parent.right }
        height: 40
        color: Qt.rgba(0.055, 0.065, 0.11, 1)

        Row {
            anchors.fill: parent
            anchors.leftMargin: 12

            Repeater {
                model: [
                    { label: "Device Name",     flex: 3 },
                    { label: "Class",           flex: 1 },
                    { label: "Manufacturer",    flex: 2 },
                    { label: "Driver Version",  flex: 1.5 },
                    { label: "Status",          flex: 1 }
                ]
                delegate: Item {
                    width: (columnHeader.width - 12) * modelData.flex / 9.5
                    height: 40
                    Label {
                        anchors { left: parent.left; verticalCenter: parent.verticalCenter }
                        text: modelData.label
                        font { pixelSize: 11; bold: true; letterSpacing: 0.3 }
                        color: Qt.rgba(0.55, 0.62, 0.78, 1)
                    }
                    Rectangle {
                        anchors { right: parent.right; verticalCenter: parent.verticalCenter }
                        width: 1; height: 20
                        color: Qt.rgba(1,1,1,0.06)
                    }
                }
            }
        }

        // bottom separator
        Rectangle {
            anchors.bottom: parent.bottom; width: parent.width; height: 1
            color: Qt.rgba(0, 0.72, 1, 0.14)
        }
    }

    // ── ListView of devices ───────────────────────────────────────────────
    ScrollView {
        anchors {
            top: columnHeader.bottom
            left: parent.left; right: parent.right; bottom: emptyState.top
        }
        contentWidth: availableWidth

        ListView {
            id: listView
            model: root.filteredModel
            clip: true

            delegate: DeviceCard {
                width: listView.width
                deviceName:    modelData.name
                deviceClass:   modelData.deviceClass
                manufacturer:  modelData.manufacturer
                driverVersion: modelData.driverVersion
                driverDate:    modelData.driverDate
                isProblem:     modelData.isProblem
                status:        modelData.status
                hardwareId:    modelData.hardwareId
                index:         model.index
            }

            // Smooth scrolling
            ScrollBar.vertical: ScrollBar { }
        }
    }

    // ── Empty state ───────────────────────────────────────────────────────
    Item {
        id: emptyState
        anchors { left: parent.left; right: parent.right; bottom: parent.bottom }
        height: filteredModel.length === 0 ? 200 : 0
        visible: filteredModel.length === 0

        Column {
            anchors.centerIn: parent
            spacing: 12

            Label {
                anchors.horizontalCenter: parent.horizontalCenter
                text: root.model.count === 0 ? "⬡" : "⊘"
                font.pixelSize: 48
                color: Qt.rgba(0.3, 0.35, 0.5, 0.6)
            }
            Label {
                anchors.horizontalCenter: parent.horizontalCenter
                text: root.model.count === 0
                      ? "No scan data — click 'Scan Hardware' to begin"
                      : "No devices match current filter"
                font.pixelSize: 13
                color: Qt.rgba(0.45, 0.5, 0.65, 1)
            }
        }
    }
}
