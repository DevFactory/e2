grammar Policy;

policy
    : definitionList pipelineList EOF
    ;

definitionList
    : definition+
    ;

definition
    : type=ID name=ID
    ;

ID
    : [a-zA-Z] [a-zA-Z0-9:_]*
    ;

pipelineList
    : pipeline+
    ;

pipeline
    : 'pipeline' filter=TC? '{'
        'inf:' inf=vertex
        'inr:' inr=vertex
        'out:' out=vertexList
        edgeList '}'
    ;

vertexList
    : vertex+
    ;


edgeList
    : edge*
    ;

edge
    : src=vertex filter=BPF? '->' dst=vertex
    ;

vertex
    : name=ID ('[' port=INT ']')
    ;

INT
    : ('0'..'9')+ ('.' ('0'..'9')+)?
    ;

BPF
    : '[' STRING ']'
    ;

TC
    : STRING
    ;

STRING
    : '"' [ A-Za-z0-9()!]+ '"'
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
