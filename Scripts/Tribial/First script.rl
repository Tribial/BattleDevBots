FUNC adult PARAMS startAge
	LET age = startAge
	LET isAdult = age >= 18
	WHILE age < 18 THEN
		PRINT "I have " + age + " years, I'm not an adult yet"
		LET age = age + 1
		LET isAdult = age >= 18
	ENDWHILE
	PRINT "I have " + age + " years, so I'm an adult now"
ENDFUNC

adult PARAMS 3