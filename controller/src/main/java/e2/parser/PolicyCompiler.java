package e2.parser;


import org.antlr.v4.runtime.ANTLRInputStream;
import org.antlr.v4.runtime.CommonTokenStream;
import org.antlr.v4.runtime.TokenStream;
import org.antlr.v4.runtime.tree.ParseTree;
import org.antlr.v4.runtime.tree.ParseTreeWalker;

import java.util.List;

import e2.pipelet.PipeletType;

public class PolicyCompiler {
    public static List<PipeletType> compile(String policy) {
        ANTLRInputStream input = new ANTLRInputStream(policy);
        PolicyLexer lexer = new PolicyLexer(input);
        TokenStream tokens = new CommonTokenStream(lexer);
        PolicyParser parser = new PolicyParser(tokens);
        ParseTree tree = parser.policy();

        ParseTreeWalker walker = new ParseTreeWalker();
        PolicyAnalyzer analyzer = new PolicyAnalyzer();

        walker.walk(analyzer, tree);
        return analyzer.PipeletTypes();
    }
}
