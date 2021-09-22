#!/bin/bash

echo Executing workload
bundle exec ruby generic-update-script.rb  |& tee output/dependabot-output.txt

echo Raising the Job Finished event
./event-handler.sh |& tee /output/event-handler-output.txt