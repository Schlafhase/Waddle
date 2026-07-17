docker build -t ssh-container ./TestServer
docker create -p 2222:22 -t --name waddle-test-server ssh-container
docker start waddle-test-server

echo
echo
echo
echo Test server started.
