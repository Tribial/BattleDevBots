LET age = 9
LET pelnoletni = age >= 18
WHILE NOT pelnoletni THEN
	PRINT "Jeszcze nie dorosly, wiek testestestestestestestestestestestsetestestestestestestesetsetestestestestestestestestestestestes " + age
	LET age = age + 1
	LET pelnoletni = age >= 18
ENDWHILE
PRINT "Teraz mam lat " + age + ", wiec jestem pelnoletni"
