/*
 * MacroUtil
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

import java.util.Map;

public class MacroUtil {
    public final static String DEFAULT_START_LITER = "${";
    public final static String DEFAULT_END_LITER = "}";

    public interface Runner {
        public String run(String macro);
    }

    public static String runMacro(String source, String startLiter, String endLiter,
            Runner runner) {
        if (source == null || source.isEmpty()) {
            return source;
        }

        if (source.indexOf(startLiter, 0) < 0) {
            return source;
        }

        int startLiterLen = startLiter.length();
        int endLiterLen = endLiter.length();
        String str = "";

        int len = source.length();
        int idx = 0;
        while (idx < len) {
            int idx2 = source.indexOf(startLiter, idx);
            if (idx2 < 0) {
                break;
            }

            if (idx < idx2) {
                str += source.substring(idx, idx2);
            }

            int idx3 = source.indexOf(endLiter, idx2);
            if (idx3 >= 0) {
                if(runner != null){
                    String label = source.substring(idx2 + startLiterLen, idx3);
                    String value = runner.run(label);
                    if (value != null) {
                        value = runMacro(value, startLiter, endLiter, runner);
                        str += value;
                    }
                }

                idx = idx3 + endLiterLen;
            } else {
                break;
            }
        }
        if (idx < len) {
            str += source.substring(idx);
        }

        return str;
    }

    private static class MacroRunner implements Runner {
        private Map<String, String> macros;

        public MacroRunner(Map<String, String> macros) {
            this.macros = macros;
        }

        @Override
        public String run(String key) {
            return macros.get(key);
        }
    }

    public static String processMacro(String source, Map<String, String> macros, String startLiter,
            String endLiter) {
        return runMacro(source, startLiter, endLiter, new MacroRunner(macros));
    }

    public static String processMacro(String source, Map<String, String> macros) {
        return processMacro(source, macros, DEFAULT_START_LITER, DEFAULT_END_LITER);
    }

    public static String processMacro(String source, String startLiter, String endLiter,
            Runner runner) {
        return runMacro(source, startLiter, endLiter, runner);
    }

    public static String processMacro(String source, Runner runner) {
        return processMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, runner);
    }
}
