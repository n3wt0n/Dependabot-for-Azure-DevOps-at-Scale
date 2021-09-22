#!/bin/bash

echo Executing workload
bundle exec ruby generic-update-script.rb  |& tee dependabot-output.txt

echo Raising the Job Finished event
./event-handler.sh |& tee event-handler-output.txt