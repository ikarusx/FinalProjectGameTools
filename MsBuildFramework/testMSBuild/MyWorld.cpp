
#include <iostream>

using std::cout;
using std::endl;

int main(void)
{
	#if DebugConfig
	cout << "WE ARE IN THE DEBUG CONFIGURATION\n";
	#endif
	
	cout << "Hello, world!\n";
	
	return 0;
}