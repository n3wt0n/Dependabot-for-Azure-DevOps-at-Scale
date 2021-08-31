:: Use this file only to create a "test" image locally. To create a production image, the GitHub Actions workflow is used instead

mkdir .\dependabotimage
cd .\dependabotimage

git clone https://github.com/dependabot/dependabot-script.git .

docker build -t "dependabot-script-local" -f Dockerfile .

cd ..
rmdir .\dependabotimage /S /Q

docker build -t "dependabot-azuredevops-atscale" -f Dockerfile.prod .