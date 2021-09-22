### WINDOWS DEVELOPERS

Before committing changes on this folder's `*.sh` files, execute:

```bash
git update-index --chmod=+x docker/event-handler.sh
git update-index --chmod=+x docker/startup-script.prd.sh
git update-index --chmod=+x docker/startup-script.test.sh
git update-index --chmod=+x docker/test-workload.sh
```

Otherwise the scripts won;t run in the PROD container