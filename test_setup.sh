git clone https://github.com/Schlafhase/Waddle waddle-tmp
cd waddle-tmp
dotnet tool install -g Waddle.Cli
docker build -t ssh-container ./TestServer/
docker create -p 2222:22 -t --name waddle-test-server ssh-container
docker start waddle-test-server
echo
echo
echo "---Waddle Test setup---------------------------"
echo "Waddle test server started. Use root@localhost:2222 with the password 'Docker!' for authentication."
echo "-----------------------------------------------"
echo
echo
waddle init
echo
echo
echo "---Waddle Test setup---------------------------"
echo "Running waddle Waddle.Cli/test which should cover most features"
echo "-----------------------------------------------"
echo
echo
waddle Waddle.Cli/test

echo
echo
echo "---Waddle Test setup---------------------------"
echo "Run ./waddle-tmp/h"
echo "-----------------------------------------------"
echo
echo
