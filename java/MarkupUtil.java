/*
 * MarkupUtil
 *
 * Copyright (c) 2000 - 2013 Samsung Electronics Co., Ltd. All rights reserved.
 *
 * Contact:
 * Gyeongmin Ju <gyeongmin.ju@samsung.com>
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Contributors:
 * - S-Core Co., Ltd
 */
package org.tizen.core.gputil;

import java.io.File;

import javax.script.Invocable;
import javax.script.ScriptEngine;
import javax.script.ScriptEngineManager;
import javax.script.ScriptException;

public class MarkupUtil {
    public final static char DEFAULT_FIRST_LITER = '?';
    public final static char DEFAULT_START_LITER = '|';
    public final static char DEFAULT_END_LITER = '*';

    public interface Runner {
        public String run(String func, String[] param);
    }

    private static class FuncToken {
        public String name = "";
        public String param = "";
    }

    public static String runMarkup(String source, char firstLiter, char startLiter, char endLiter, Runner[] runners) {
        if (source == null || source.isEmpty()) {
            return source;
        }

        if (source.indexOf(firstLiter, 0) < 0) {
            return source;
        }

        String str = "";
        String tempStr = "";

        int stackIdx = 0;

        int len = source.length();
        FuncToken[] stack = new FuncToken[len];
        FuncToken func = null;

        int idx = 0;
        int mode = 0;
        while (idx < len) {
            char ch = source.charAt(idx);
            idx++;

            switch (mode) {
            case 0:
                if (ch == firstLiter) {
                    func = new FuncToken();
                    mode = 1;
                } else {
                    str += ch;
                }
                break;
            case 1:
                tempStr += ch;
                if (ch == startLiter) {
                    mode = 2;
                } else {
                    func.name += ch;
                }
                break;
            case 2:
                tempStr += ch;
                if (ch == endLiter) {
                    String[] params = func.param.split(",");
                    String runStr = null;
                    for (Runner runner : runners) {
                        runStr = runner.run(func.name, params);
                        if (runStr != null) {
                            break;
                        }
                    }
                    if (stackIdx > 0) {
                        func = stack[--stackIdx];
                        if (runStr != null) {
                            func.param += runStr;
                        }
                        mode = 2;
                    } else {
                        if (runStr != null) {
                            str += runStr;
                        }
                        mode = 0;
                        func = null;
                        tempStr = "";
                    }
                } else if (ch == firstLiter) {
                    stack[stackIdx++] = func;
                    func = new FuncToken();
                    mode = 1;
                } else {
                    func.param += ch;
                }
                break;
            }
        }

        if (!tempStr.isEmpty()) {
            str += tempStr;
        }

        return str;
    }

    public static String runMarkup(String source, char firstLiter, char startLiter, char endLiter, Runner runner) {
        return runMarkup(source, firstLiter, startLiter, endLiter, new Runner[] { runner });
    }

    private static Runner defaultMarkupRunner = null;

    public static Runner getDefaultMarkupRunner() {
        if (defaultMarkupRunner == null) {
            defaultMarkupRunner = new Runner() {
                @Override
                public String run(String func, String[] param) {
                    String value = null;
                    if (func.equals("if")) {
                        if(param[0] != null && !param[0].isEmpty() && param.length >= 2) {
                            return param[1];
                        } else {
                            return (param.length >= 3) ? param[2] : null;
                        }
                    } else if (func.equals("dir")) {
                        File file = new File(param[0]);
                        if (file.exists()) {
                            value = file.getParent();
                        }
                    } else if (func.equals("file")) {
                        File file = new File(param[0]);
                        if (file.exists()) {
                            value = file.toString();
                        }
                    } else if (func.equals("filename")) {
                        File file = new File(param[0]);
                        if (file.exists()) {
                            value = file.getName();
                        }
                    } else if (func.equals("relative")) {
                        return PathUtil.getRelativePath(param[0], param[1]);
                    } else if (func.equals("select")) {
                        for (String p : param) {
                            if (p != null && !p.isEmpty()) {
                                value = p;
                                break;
                            }
                        }
                    }
                    return value;
                }
            };
        }

        return defaultMarkupRunner;
    }

    public static String processDefaultMarkup(String source) {
        return runMarkup(source, DEFAULT_FIRST_LITER, DEFAULT_START_LITER, DEFAULT_END_LITER, getDefaultMarkupRunner());
    }

    public static Runner getJavaScriptMarkupRunner(String script) {
        ScriptEngine js = new ScriptEngineManager().getEngineByName("javascript");
        // Bindings bindings = js.getBindings(ScriptContext.ENGINE_SCOPE);
        // bindings.put("stdout", System.out);
        try {
            js.eval(script);
        } catch (ScriptException e) {
            e.printStackTrace();
        }
        final Invocable invocableEngine = (Invocable) js;

        Runner runner = new Runner() {
            @Override
            public String run(String func, String[] param) {
                String ret = null;
                try {
                    ret = (String) invocableEngine.invokeFunction(func, (Object[]) param);
                } catch (NoSuchMethodException e) {
                    // do nothing
                } catch (ScriptException e) {
                    e.printStackTrace();
                }
                return ret;
            }
        };

        return runner;
    }

    public static String processMarkupByJavaScript(String source, String javaScript) {
        return runMarkup(source, DEFAULT_FIRST_LITER, DEFAULT_START_LITER, DEFAULT_END_LITER,
                getJavaScriptMarkupRunner(javaScript));
    }

    public static String processDefaultMarkup2(String source, Runner[] extRunners) {
        Runner[] runners = new Runner[1 + extRunners.length];
        runners[0] = getDefaultMarkupRunner();
        int idx = 1;
        for (Runner runner : extRunners) {
            runners[idx++] = runner;
        }
        return runMarkup(source, DEFAULT_FIRST_LITER, DEFAULT_START_LITER, DEFAULT_END_LITER, runners);
    }
}
