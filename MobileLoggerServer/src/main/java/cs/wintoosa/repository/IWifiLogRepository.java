/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package cs.wintoosa.repository;

import cs.wintoosa.domain.WifiLog;
import org.springframework.data.jpa.repository.JpaRepository;

/**
 *
 * @author vkukkola
 */
public interface IWifiLogRepository extends JpaRepository<WifiLog, Long>, IWifiLogRepositoryCustom{
    
}
