docker rm -f waddle-test-server
dotnet tool uninstall -g Waddle.Cli
cd ..
rm -r waddle-tmp
rm test_setup.sh
