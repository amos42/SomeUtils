/*
 * IDE
 *
 * Copyright (c) 2000 - 2013 Samsung Electronics Co., Ltd. All rights reserved.
 *
 * Contact:
 * Hyunsik Noh <hyunsik.noh@samsung.com>
 * Hyeongseok Heo <hyeongseok.heo@samsung.com>
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

public class VersionUtil {
    public final static String VERSION_LITER = ".";
    public final static String VERSION_LITER_REGX = "\\.";

    public static int compareVersion(String version, String orgVersion, int available) {
        if (version == null) {
            return 0;
        } else if (orgVersion == null) {
            return 1;
        }
        
        String[] vers = version.split(VERSION_LITER_REGX);
        String[] vers0 = orgVersion.split(VERSION_LITER_REGX);

        int len = (vers.length >= vers0.length) ? vers.length : vers0.length;
        if (available > 0 && len > available) {
            len = available;
        }

        int v, v0;
        for (int i = 0; i < len; i++) {
            if (i < vers.length) {
                v = (vers[i].isEmpty()) ? 0 : Integer.valueOf(vers[i]);
            } else {
                v = 0;
            }
            if (i < vers0.length) {
                v0 = (vers0[i].isEmpty()) ? 0 : Integer.valueOf(vers0[i]);
            } else {
                v0 = 0;
            }

            if (v < 0) {
            	return -1;
            } else if (v > v0) {
                return 1;
            } else if (v == v0) {
                continue;
            } else {
                return 0;
            }
        }

        return 0;
    }

    public static int compareVersion(String version, String orgVersion) {
        return compareVersion(version, orgVersion, 0);
    }
}
