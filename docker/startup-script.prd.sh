echo Executing workload
bundle exec ruby generic-update-script.rb

echo Raising the Job Finished event
./event-handler.sh