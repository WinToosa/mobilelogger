package cs.wintoosa.domain;

import java.io.Serializable;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.Inheritance;
import javax.persistence.InheritanceType;
import javax.persistence.ManyToOne;
import javax.persistence.Transient;
import javax.validation.constraints.NotNull;
import org.codehaus.jackson.annotate.JsonIgnore;

/**
 *
 * @author jonimake
 */
@Entity
@Inheritance(strategy= InheritanceType.TABLE_PER_CLASS)
public abstract class Abstractlog implements Serializable{

    private static final long serialVersionUID = 1234l;
    
    @Id
    @GeneratedValue(strategy= GenerationType.AUTO)
    protected Long id;
    
    @ManyToOne(targetEntity=Sessionlog.class)
    @JsonIgnore
    protected Sessionlog sessionlog;
        
    @NotNull(message="PhoneId can't be null")
    protected String phoneId;
    
    @NotNull (message="timestamp must represent valid unixtime in seconds")
    protected Long timestamp;
    
    @Transient
    private String checksum;
       
    public Abstractlog() {
    }
    
    public String getPhoneId() {
        return phoneId;
    }

    public void setPhoneId(String id) {
        this.phoneId = id;
    }
    public Long getId() {
        return id;
    }

    public void setId(Long logId) {
        this.id = logId;
    }

    public Long getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(Long timestamp) {
        this.timestamp = timestamp;
    }

    public String getChecksum() {
        return checksum;
    }

    public void setChecksum(String checksum) {
        this.checksum = checksum;
    }

    @JsonIgnore
    public Sessionlog getSessionlog() {
        return sessionlog;
    }

    @JsonIgnore
    public void setSessionlog(Sessionlog sessionlog) {
        this.sessionlog = sessionlog;
    }

}
