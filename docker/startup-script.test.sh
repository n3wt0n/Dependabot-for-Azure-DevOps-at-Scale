#!/bin/bash

echo Executing workload
test-workload.sh

echo Raising the Job Finished event
event-handler.sh