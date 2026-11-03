cmake_minimum_required(VERSION 3.25)

project(OpenDriverUpdater VERSION 2.0.0 LANGUAGES CXX C)

list(APPEND CMAKE_PREFIX_PATH "C:/Qt/6.8.1/msvc2022_64")

add_subdirectory(src/gui_qt)
