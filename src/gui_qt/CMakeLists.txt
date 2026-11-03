cmake_minimum_required(VERSION 3.25)

project(OpenDriverUpdaterGui LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)

find_package(Qt6 COMPONENTS Core Gui Quick QuickControls2 Concurrent Svg REQUIRED)

qt_standard_project_setup(REQUIRES 6.8)

qt_add_executable(OpenDriverUpdaterGui WIN32
    main.cpp
    corebridge.h
    corebridge.cpp
)

# ── QML module — RESOURCE_PREFIX "/" makes qrc path match qrc:/OpenDriverUpdater/qml/Main.qml
qt_add_qml_module(OpenDriverUpdaterGui
    URI OpenDriverUpdater
    VERSION 1.0
    RESOURCE_PREFIX "/"
    QML_FILES
        qml/Main.qml
        qml/DeviceCard.qml
        qml/ScanButton.qml
        qml/StatusBar.qml
        qml/DeviceTableView.qml
        qml/UpdatesView.qml
)

# ── Separate resource target for static assets (icons, etc.)
qt_add_resources(OpenDriverUpdaterGui "app_assets"
    PREFIX "/"
    FILES
        ../../img/odu_favicon.ico
)

target_link_libraries(OpenDriverUpdaterGui PRIVATE
    Qt6::Core
    Qt6::Gui
    Qt6::Quick
    Qt6::QuickControls2
    Qt6::Concurrent
    Qt6::Svg
)

# ── Copy odu_core.dll to binary folder post-build if present
if(EXISTS "${CMAKE_SOURCE_DIR}/src/core_rust/target/release/odu_core.dll")
    add_custom_command(TARGET OpenDriverUpdaterGui POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_SOURCE_DIR}/src/core_rust/target/release/odu_core.dll"
            "$<TARGET_FILE_DIR:OpenDriverUpdaterGui>/odu_core.dll"
        COMMENT "Copying odu_core.dll to target folder..."
    )
endif()

# ── Copy OpenDriverUpdater.Bridge.dll to binary folder post-build if present
if(EXISTS "${CMAKE_SOURCE_DIR}/src/OpenDriverUpdater.Bridge/bin/Debug/net8.0-windows10.0.19041.0/OpenDriverUpdater.Bridge.dll")
    add_custom_command(TARGET OpenDriverUpdaterGui POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_SOURCE_DIR}/src/OpenDriverUpdater.Bridge/bin/Debug/net8.0-windows10.0.19041.0/OpenDriverUpdater.Bridge.dll"
            "$<TARGET_FILE_DIR:OpenDriverUpdaterGui>/OpenDriverUpdater.Bridge.dll"
        COMMENT "Copying OpenDriverUpdater.Bridge.dll to target folder..."
    )
endif()
