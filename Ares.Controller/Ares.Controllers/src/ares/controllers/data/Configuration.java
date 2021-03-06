/*
 Copyright (c) 2010 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
package ares.controllers.data;

import java.util.ArrayList;
import java.util.List;

public final class Configuration {

  private List<Mode> modes;
  private String title;
  
  public Configuration() {
    modes = new ArrayList<Mode>();
  }
  
  public void setTitle(String title) {
	  this.title = title;
  }
  
  public String getTitle() {
	  return title;
  }
  
  public void addMode(Mode mode) {
    modes.add(mode);
  }
  
  public List<Mode> getModes() {
    return java.util.Collections.unmodifiableList(modes);
  }
  
  public String getCommandTitle(int commandId) {
	  for (Mode mode : modes) {
		  String title = mode.getTitle(commandId);
		  if (title != null) {
			  return title;
		  }
	  }
	  return null;
  }
  
  public boolean containsKeyStroke(KeyStroke keyStroke) {
	if (keyStroke == null)
		return false;
    for(Command command : modes) {
      if (command.getKeyStroke() != null && command.getKeyStroke().equals(keyStroke)) return true;
    }
    return false;
  }
}
