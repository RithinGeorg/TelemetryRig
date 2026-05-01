cmake_minimum_required(VERSION 3.20)
project(TelemetryRigNative LANGUAGES CXX)

add_library(TelemetryRig.Native SHARED TelemetryMath.cpp)
set_target_properties(TelemetryRig.Native PROPERTIES CXX_STANDARD 17 CXX_STANDARD_REQUIRED YES)
