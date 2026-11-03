#include "mainwindow.h"
#include <QApplication>

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent), m_bridge(new CoreBridge(this))
{
    setWindowTitle("OpenDriverUpdater v2.0 • Qt 6.8 Avant-Garde");
    resize(1100, 720);

    setupUi();
    applyTheme();

    connect(m_bridge, &CoreBridge::devicesScanned, this, &MainWindow::onDevicesScanned);
}

void MainWindow::setupUi() {
    auto *centralWidget = new QWidget(this);
    setCentralWidget(centralWidget);

    auto *mainLayout = new QVBoxLayout(centralWidget);
    mainLayout->setContentsMargins(24, 24, 24, 24);
    mainLayout->setSpacing(20);

    // Top Header Banner
    auto *headerFrame = new QFrame(this);
    headerFrame->setObjectName("HeaderFrame");
    auto *headerLayout = new QHBoxLayout(headerFrame);
    headerLayout->setContentsMargins(20, 16, 20, 16);

    auto *titleBox = new QVBoxLayout();
    auto *titleLabel = new QLabel("OPEN DRIVER UPDATER", headerFrame);
    titleLabel->setObjectName("TitleLabel");
    auto *subtitleLabel = new QLabel("QT 6.8 MSVC2022 • RUST CORE ENGINE", headerFrame);
    subtitleLabel->setObjectName("SubtitleLabel");
    titleBox->addWidget(titleLabel);
    titleBox->addWidget(subtitleLabel);

    m_scanBtn = new QPushButton("INITIATE HARDWARE DIAGNOSTIC", headerFrame);
    m_scanBtn->setObjectName("ScanButton");
    m_scanBtn->setCursor(Qt::PointingHandCursor);
    connect(m_scanBtn, &QPushButton::clicked, this, &MainWindow::onScanButtonClicked);

    headerLayout->addLayout(titleBox);
    headerLayout->addStretch();
    headerLayout->addWidget(m_scanBtn);

    // Status Banner
    auto *statusBox = new QHBoxLayout();
    m_statusLabel = new QLabel("System ready. Select diagnostic to scan devices.", this);
    m_statusLabel->setObjectName("StatusLabel");
    m_summaryLabel = new QLabel("Node Count: 0", this);
    m_summaryLabel->setObjectName("SummaryLabel");
    statusBox->addWidget(m_statusLabel);
    statusBox->addStretch();
    statusBox->addWidget(m_summaryLabel);

    m_progressBar = new QProgressBar(this);
    m_progressBar->setObjectName("ScanProgressBar");
    m_progressBar->setRange(0, 0); // Indeterminate
    m_progressBar->setFixedHeight(3);
    m_progressBar->hide();

    // Device Tree Widget
    m_deviceTree = new QTreeWidget(this);
    m_deviceTree->setObjectName("DeviceTree");
    m_deviceTree->setColumnCount(5);
    m_deviceTree->setHeaderLabels({"Hardware Device Name", "Class", "Manufacturer", "Driver Version", "Status"});
    m_deviceTree->header()->setSectionResizeMode(0, QHeaderView::Stretch);
    m_deviceTree->header()->setSectionResizeMode(1, QHeaderView::ResizeToContents);
    m_deviceTree->header()->setSectionResizeMode(2, QHeaderView::ResizeToContents);
    m_deviceTree->header()->setSectionResizeMode(3, QHeaderView::ResizeToContents);
    m_deviceTree->header()->setSectionResizeMode(4, QHeaderView::ResizeToContents);
    m_deviceTree->setAlternatingRowColors(true);

    mainLayout->addWidget(headerFrame);
    mainLayout->addLayout(statusBox);
    mainLayout->addWidget(m_progressBar);
    mainLayout->addWidget(m_deviceTree, 1);
}

void MainWindow::applyTheme() {
    QString qss = R"(
        QMainWindow {
            background-color: #090a10;
        }
        #HeaderFrame {
            background: qlineargradient(x1:0, y1:0, x2:1, y2:0, stop:0 #121524, stop:1 #1a1e36);
            border: 1px solid #282e52;
            border-radius: 12px;
        }
        #TitleLabel {
            color: #ffffff;
            font-size: 22px;
            font-weight: bold;
            letter-spacing: 2px;
        }
        #SubtitleLabel {
            color: #00f0ff;
            font-size: 11px;
            font-weight: 600;
            letter-spacing: 1px;
        }
        #ScanButton {
            background-color: #0078d4;
            color: #ffffff;
            font-weight: bold;
            font-size: 12px;
            padding: 12px 24px;
            border-radius: 6px;
            border: none;
        }
        #ScanButton:hover {
            background-color: #0086ed;
        }
        #ScanButton:disabled {
            background-color: #2b324d;
            color: #6c779e;
        }
        #StatusLabel {
            color: #a0aabf;
            font-size: 13px;
        }
        #SummaryLabel {
            color: #00f0ff;
            font-size: 13px;
            font-weight: bold;
        }
        #ScanProgressBar {
            background-color: #121524;
            border: none;
        }
        #ScanProgressBar::chunk {
            background-color: #00f0ff;
        }
        QTreeWidget#DeviceTree {
            background-color: #0e111d;
            border: 1px solid #1f253e;
            border-radius: 8px;
            color: #d1d9e6;
            font-size: 13px;
            gridline-color: #1a2035;
        }
        QTreeWidget#DeviceTree::item {
            padding: 8px;
        }
        QTreeWidget#DeviceTree::item:alternate {
            background-color: #111524;
        }
        QTreeWidget#DeviceTree::item:selected {
            background-color: #1e2645;
            color: #00f0ff;
        }
        QHeaderView::section {
            background-color: #141829;
            color: #8c9bb5;
            padding: 10px;
            font-weight: bold;
            border: none;
            border-bottom: 1px solid #232a48;
        }
    )";
    setStyleSheet(qss);
}

void MainWindow::onScanButtonClicked() {
    m_scanBtn->setEnabled(false);
    m_progressBar->show();
    m_statusLabel->setText("Interrogating Windows WMI endpoints via Rust Core...");
    m_deviceTree->clear();

    m_bridge->scanDevicesAsync();
}

void MainWindow::onDevicesScanned(const QJsonArray &devices) {
    m_progressBar->hide();
    m_scanBtn->setEnabled(true);

    m_statusLabel->setText("Diagnostic complete. Hardware inventory updated.");
    m_summaryLabel->setText(QString("Node Count: %1").arg(devices.size()));

    for (const QJsonValue &val : devices) {
        QJsonObject obj = val.toObject();
        auto *item = new QTreeWidgetItem(m_deviceTree);
        item->setText(0, obj["name"].toString());
        item->setText(1, obj["device_class"].toString());
        item->setText(2, obj["manufacturer"].toString());
        item->setText(3, obj["driver_version"].toString());

        bool isProblem = obj["is_problem"].toBool();
        if (isProblem) {
            item->setText(4, "⚠️ Warning / Problem");
            item->setForeground(4, QBrush(QColor("#ff4d4d")));
        } else {
            item->setText(4, "✓ OK");
            item->setForeground(4, QBrush(QColor("#00e676")));
        }
    }
}
