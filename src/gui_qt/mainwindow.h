#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QTreeWidget>
#include <QPushButton>
#include <QLabel>
#include <QProgressBar>
#include <QHeaderView>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QFrame>
#include <QJsonArray>
#include <QJsonObject>
#include "corebridge.h"

class MainWindow : public QMainWindow {
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);
    ~MainWindow() override = default;

private slots:
    void onScanButtonClicked();
    void onDevicesScanned(const QJsonArray &devices);

private:
    void setupUi();
    void applyTheme();

    CoreBridge *m_bridge;
    QLabel *m_statusLabel;
    QLabel *m_summaryLabel;
    QProgressBar *m_progressBar;
    QPushButton *m_scanBtn;
    QTreeWidget *m_deviceTree;
};

#endif // MAINWINDOW_H
