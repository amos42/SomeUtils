/*
 *  PathUtil.java
 *
 * Copyright (c) 2000 - 2016 Samsung Electronics Co., Ltd. All rights reserved.
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

public class PathUtil {
    public static boolean isAbsolutePath(String filePath) {
        return new File(filePath).isAbsolute();
    }

    public static String getFullPath(String rootPath, String filePath) {
        if(rootPath == null) {
            return filePath;
        }

        if (filePath == null) {
            return rootPath;
        }
        
        if (new File(filePath).isAbsolute()) {
            return filePath;
        }

        return new File(rootPath, filePath).toString();
    }

    public static String getRelativePath(String rootPath, String filePath) {
        if (rootPath == null) {
            return filePath;
        }

        if (filePath == null) {
            return rootPath;
        }
        
        //String sep = File.separator;
        String sep = "/";
        rootPath = rootPath.replace('\\', '/');
        filePath = filePath.replace('\\', '/');

        if (rootPath.equals(filePath)) {
            return ".";
        }

        String[] rootPaths = rootPath.split(sep);
        String[] filePaths = filePath.split(sep);

        if (filePaths.length <= 0) {
            return filePath;
        }

        int len = 0;
        for (int i = 0; i < filePaths.length; i++) {
            len = i;
            if (i >= rootPaths.length || !filePaths[i].equalsIgnoreCase(rootPaths[i])) {
                break;
            }
        }

        String pre = "";
        for (int i = 0; i < rootPaths.length - len; i++) {
            if (!pre.isEmpty()) {
                pre += sep;
            }
            pre += "..";
        }
        for (int i = len; i < filePaths.length; i++) {
            if (!pre.isEmpty()) {
                pre += sep;
            }
            pre += filePaths[i];
        }

        return pre;
    }

    public static String trim(String dir) {
        if(dir.endsWith("/.") || dir.endsWith("\\.")) {
            dir = dir.substring(0, dir.length() - 2);
        }        
        if(dir.length() > 1 && dir.endsWith("/") || dir.endsWith("\\")) {
            dir = dir.substring(0, dir.length() - 1);
        }
        
        return dir;
    }
    
    public static String addPath(String path, String... paths) {
    	for(String p : paths) {
    		path += File.separator + p;
    	}
    	
    	return path;
    }

}
