/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package cs.wintoosa.domain;

import javax.persistence.Entity;

/**
 *
 * @author vkukkola
 */
@Entity
public class AccLog extends Log{
    
    private Float accX;
    
    private Float accY;
    
    private Float accZ;

    public Float getAccX() {
        return accX;
    }

    public void setAccX(Float X) {
        this.accX = X;
    }

    public Float getAccY() {
        return accY;
    }

    public void setAccY(Float Y) {
        this.accY = Y;
    }

    public Float getAccZ() {
        return accZ;
    }

    public void setAccZ(Float Z) {
        this.accZ = Z;
    }
    
}
