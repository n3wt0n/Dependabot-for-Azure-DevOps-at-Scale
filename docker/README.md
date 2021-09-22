### WINDOWS DEVELOPERS

Before committing changes on this folder's `*.sh` files, execute:

```bash
git update-index --chmod=+x docker/event-handler.sh
git update-index --chmod=+x docker/startup-script.prd.sh
git update-index --chmod=+x docker/startup-script.test.sh
git update-index --chmod=+x docker/test-workload.sh
```

To check permissions:

```bash
git ls-files -s -- *.sh
```

All files should have permission `100755`

Otherwise the scripts won;t run in the PROD container