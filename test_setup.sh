docker build -t ssh-container ./TestServer/
docker create -p 2222:22 -t --name waddle-test-server ssh-container
docker start waddle-test-server
echo "Waddle test server started. Use root@localhost:22 with the password 'Docker!' for authentication."
waddle init
echo "Run waddle Waddle.Cli/test to run the test workflow which should cover most features"
