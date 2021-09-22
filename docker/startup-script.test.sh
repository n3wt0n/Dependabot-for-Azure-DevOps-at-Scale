#!/bin/sh

echo Executing workload
/Test/test-workload.sh |& tee dependabot-output.txt

echo Raising the Job Finished event
/Test/event-handler.sh |& tee event-handler-output.txt