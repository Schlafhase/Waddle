git clone https://github.com/Schlafhase/Waddle waddle-tmp
cd waddle-tmp
dotnet tool install -g Waddle.Cli
docker build -t ssh-container ./TestServer/
docker create -p 2222:22 -t --name waddle-test-server ssh-container
docker start waddle-test-server
echo
echo
echo "---Waddle Test setup---------------------------"
echo "Waddle test server started. Use these settings:"
echo "Host: localhost"
echo "Username: root"
echo "Port: 2222"
echo
echo "When asked for a password, use 'Docker!'"
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
