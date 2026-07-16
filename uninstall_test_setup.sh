docker rm -f waddle-test-server
dotnet tool uninstall -g Waddle.Cli
rm -rf waddle-tmp
rm test_setup.sh

echo
echo
echo "---Waddle Test setup---------------------------------------------------"
echo "Waddle test setup removed"
echo "-----------------------------------------------------------------------"
echo
echo
