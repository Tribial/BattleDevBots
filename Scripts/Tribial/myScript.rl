FUNC writeMyName PARAMS firstName lastName
	PRINT "Hi, I'm " + firstName + " " + lastName
ENDFUNC
FUNC addTwoNumbers PARAMS number1 number2
	PRINT number1 + number2
ENDFUNC
LET fabian = "Fabian"
LET num = 5
LET num2 = 10
writeMyName PARAMS fabian "Domurad"
writeMyName PARAMS "Alicja" "Zalewska"
writeMyName PARAMS "Przemcio" "Budzich"
addTwoNumbers PARAMS num num2
LET num = num + 15
addTwoNumbers PARAMS num 12